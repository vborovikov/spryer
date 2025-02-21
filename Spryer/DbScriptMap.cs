﻿namespace Spryer;

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FrozenScripts = System.Collections.Frozen.FrozenDictionary<string, string>;
using MutableScripts = System.Collections.Generic.Dictionary<string, string>;

/// <summary>
/// Represents a collection of SQL scripts loaded from an external source.
/// </summary>
[DebuggerDisplay("{Source}/{Version}: {Count}")]
public sealed class DbScriptMap
{
    private const string ScriptsFileName = "Scripts";
    private const string ScriptsFileExt = ".sql";

    /// <summary>
    /// An empty instance of <see cref="DbScriptMap"/>.
    /// </summary>
    public static readonly DbScriptMap Empty = new(string.Empty, new(), FrozenScripts.Empty);

    private readonly string source;
    private readonly Version version;
    private readonly FrozenScripts scripts;

    private DbScriptMap(string source, Version version, FrozenScripts scripts)
    {
        this.source = source;
        this.version = version;
        this.scripts = scripts;
    }

    /// <summary>
    /// Gets the SQL script with the specified name.
    /// </summary>
    /// <param name="name">A script name.</param>
    /// <returns>A SQL script with the specified name.</returns>
    public string this[string name]
    {
        get => this.scripts.GetValueOrDefault(name, string.Empty);
    }

    /// <summary>
    /// Gets the source for scripts in the collection.
    /// </summary>
    public string Source => this.source;

    /// <summary>
    /// Gets the version of the collection.
    /// </summary>
    public Version Version => this.version;

    /// <summary>
    /// Gets the number of scripts in the collection.
    /// </summary>
    public int Count => this.scripts.Count;

    /// <summary>
    /// Loads a collection of SQL scripts from an external source.
    /// </summary>
    /// <param name="fileName">A script file name.</param>
    /// <returns>A collection of SQL scripts.</returns>
    public static DbScriptMap Load(string? fileName = null)
    {
        var loader = new Loader { FileName = fileName };

        loader.Assembly = Assembly.GetCallingAssembly();
        loader.TryLoadScripts();

        loader.Assembly = Assembly.GetEntryAssembly();
        loader.TryLoadScripts();

        loader.Assembly = null;
        loader.TryLoadScripts();

        return loader.GetScriptMap();
    }

    /// <summary>
    /// Represents a loader for the script collection.
    /// </summary>
    public class Loader
    {
        private readonly MutableScripts scripts;
        private readonly StringBuilder source;
        private Version version;

        /// <summary>
        /// Creates a new <see cref="Loader"/> instance.
        /// </summary>
        public Loader()
        {
            this.scripts = new(StringComparer.OrdinalIgnoreCase);
            this.source = new();
            this.version = Empty.Version;
        }

        /// <summary>
        /// Gets or sets the desired file name fro the script collection.
        /// </summary>
        public string? FileName { get; init; }

        /// <summary>
        /// Gets or sets the assembly to look for the script collection.
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// Gets the loaded script collection.
        /// </summary>
        public DbScriptMap GetScriptMap()
        {
            if (this.scripts.Count > 0)
            {
                return new(this.source.ToString(), this.version, this.scripts.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
            }

            return Empty;
        }

        /// <summary>
        /// Tries to load the script collection.
        /// </summary>
        /// <returns><c>true</c> if the script collection is successfully loaded, <c>false</c> otherwise.</returns>
        public bool TryLoadScripts()
        {
            var count = 0;

            if (this.FileName.HasWildcard())
            {
                // loading every file that matches the file name pattern in order:

                // - assembly resources
                if (this.Assembly is not null)
                {
                    var resourceNames = this.Assembly.GetManifestResourceNames();
                    var commonPrefixLength = resourceNames.CommonPrefixLength();
                    foreach (var resourceName in resourceNames)
                    {
                        var resourceNameSpan = resourceName.AsSpan();
                        if (commonPrefixLength > 0)
                            resourceNameSpan = resourceNameSpan[commonPrefixLength..];

                        if (FileSystemName.MatchesSimpleExpression(this.FileName, resourceNameSpan) &&
                            this.Assembly.GetManifestResourceStream(resourceName) is Stream resourceStream &&
                            TryLoadStream(resourceName, resourceStream))
                        {
                            ++count;
                        }
                    }
                }

                // - app directories
                foreach (var scriptFilePath in FindFilePaths(this.FileName))
                {
                    if (TryLoadStream(scriptFilePath, File.OpenRead(scriptFilePath)))
                        ++count;
                }
            }
            else
            {
                // loading any file that has the given file name or the default one

                foreach (var scriptFileName in EnumerateFileNames())
                {
                    if (string.IsNullOrWhiteSpace(scriptFileName))
                        continue;

                    // every file from the assembly resources
                    if (this.Assembly is not null &&
                        Array.Find(this.Assembly.GetManifestResourceNames(), n => n.EndsWith(scriptFileName, StringComparison.OrdinalIgnoreCase)) is string resourceName &&
                        this.Assembly.GetManifestResourceStream(resourceName) is Stream resourceStream &&
                        TryLoadStream(resourceName, resourceStream))
                    {
                        ++count;
                    }

                    // first existing file from the app directories
                    foreach (var scriptFilePath in EnumerateFilePaths(scriptFileName))
                    {
                        Debug.WriteLine($"Spryer: looking for '{scriptFilePath}'");

                        if (File.Exists(scriptFilePath) && TryLoadStream(scriptFilePath, File.OpenRead(scriptFilePath)))
                        {
                            ++count;
                            break;
                        }
                    }
                }
            }

            return count > 0;
        }

        private bool TryLoadStream(string scriptSource, Stream scriptStream)
        {
            var scriptText = GetScriptText(scriptStream);

            if (!string.IsNullOrWhiteSpace(scriptText))
            {
                var scriptCount = ParseScripts(scriptText);
                Debug.WriteLine($"Spryer: {scriptCount} scripts found in '{scriptSource}'");
                if (scriptCount > 0)
                {
                    if (this.source.Length > 0) this.source.AppendLine();
                    this.source.Append(scriptSource);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads the script text from the given stream.
        /// </summary>
        /// <param name="scriptStream">The script stream.</param>
        /// <returns>A script raw text.</returns>
        protected virtual string? GetScriptText(Stream scriptStream)
        {
            using var reader = new StreamReader(scriptStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            return reader.ReadToEnd();
        }

        private IEnumerable<string> FindFilePaths(string fileName)
        {
            if (this.Assembly is not null)
            {
                // local app data: %AppData%\Local\<Assembly>\
                var localAppData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    this.Assembly.GetName().Name!);
                if (Directory.Exists(localAppData))
                {
                    foreach (var foundFileName in Directory.EnumerateFiles(localAppData, fileName))
                        yield return foundFileName;
                }

                // assembly directory
                if (Path.GetDirectoryName(this.Assembly.Location) is { Length: > 0 } assemblyDir)
                {
                    foreach (var foundFileName in Directory.EnumerateFiles(assemblyDir, fileName))
                        yield return foundFileName;
                }
            }
            else
            {
                // local app data: %AppData%\Local\<Process>\
                if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) is { Length: > 0 } processName)
                {
                    var localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), processName);
                    if (Directory.Exists(localAppData))
                    {
                        foreach (var foundFileName in Directory.EnumerateFiles(localAppData, fileName))
                            yield return foundFileName;
                    }
                }

                // process directory
                if (Path.GetDirectoryName(Environment.ProcessPath) is { Length: > 0 } processDir)
                {
                    foreach (var foundFileName in Directory.EnumerateFiles(processDir, fileName))
                        yield return foundFileName;
                }
            }
        }

        private IEnumerable<string> EnumerateFilePaths(string scriptFileName)
        {
            if (this.Assembly is not null)
            {
                // assembly directory
                if (Path.GetDirectoryName(this.Assembly.Location) is { Length: > 0 } assemblyDir)
                    yield return Path.Combine(assemblyDir, scriptFileName);

                // local app data: %AppData%\Local\<Assembly>\
                yield return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), this.Assembly.GetName().Name!),
                    scriptFileName);
            }
            else
            {
                // process directory
                if (Path.GetDirectoryName(Environment.ProcessPath) is { Length: > 0 } processDir)
                    yield return Path.Combine(processDir, scriptFileName);

                // local app data: %AppData%\Local\<Process>\
                if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) is { Length: > 0 } processName)
                {
                    yield return Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), processName),
                        scriptFileName);
                }
            }
        }

        private IEnumerable<string?> EnumerateFileNames()
        {
            if (!string.IsNullOrWhiteSpace(this.FileName) && !this.FileName.HasWildcard())
            {
                yield return this.FileName;
            }
            else
            {
                yield return Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ScriptsFileExt);
                yield return Path.GetFileNameWithoutExtension(Environment.ProcessPath) + "." + ScriptsFileName + ScriptsFileExt;

                if (this.Assembly is not null)
                {
                    yield return this.Assembly.GetName().Name + ScriptsFileExt;
                    yield return this.Assembly.GetName().Name + "." + ScriptsFileName + ScriptsFileExt;
                    yield return Path.ChangeExtension(Path.GetFileName(this.Assembly.Location), ScriptsFileExt);
                }

                yield return ScriptsFileName + ScriptsFileExt;
            }
        }

        private int ParseScripts(ReadOnlySpan<char> text)
        {
            // any sql statements before the first pragma are ignored
            var start = Pragma.FindMarkerIndex(text);
            if (start < 0)
            {
                return 0;
            }

            text = text[start..];

            var count = 0;
            foreach (var pragma in Pragma.Enumerate(text))
            {
                if (pragma.Name.Equals(Pragma.Script, StringComparison.OrdinalIgnoreCase))
                {
                    ref var script = ref CollectionsMarshal.GetValueRefOrAddDefault(this.scripts, pragma.Meta.ToString(), out _);
                    script = pragma.Data.ToString();
                    ++count;
                }
                else if (pragma.Name.Equals(Pragma.Version, StringComparison.OrdinalIgnoreCase))
                {
                    if (Version.TryParse(pragma.Meta, out var foundVersion) && foundVersion > this.version)
                    {
                        this.version = foundVersion;
                    }
                }
            }

            return count;
        }
    }

    [DebuggerDisplay("@{Name,nq} {Meta,nq}")]
    private readonly ref struct Pragma
    {
        public const string Marker = "--@";

        public const string Script = "script";
        public const string Version = "version";

        public Pragma(ReadOnlySpan<char> name, ReadOnlySpan<char> meta, ReadOnlySpan<char> data)
        {
            this.Name = name;
            this.Meta = meta;
            this.Data = data;
        }

        public ReadOnlySpan<char> Name { get; }
        public ReadOnlySpan<char> Meta { get; }
        public ReadOnlySpan<char> Data { get; }

        public static PragmaEnumerator Enumerate(ReadOnlySpan<char> text) => new(text);

        public static int FindMarkerIndex(ReadOnlySpan<char> span)
        {
            var offset = 0;
            var index = -1;

            while (span.Length > 0)
            {
                index = span.IndexOf(Marker, StringComparison.Ordinal);
                if (index == 0)
                    break;
                if (index < 0)
                    return -1;

                if (index > 0)
                {
                    if (span[index - 1] is '\n' or '\r')
                    {
                        break;
                    }

                    offset += index;
                    span = span[(index + 1)..];
                }
            }

            return offset + index;
        }
    }

    private ref struct PragmaEnumerator
    {
        private ReadOnlySpan<char> text;
        private Pragma current;

        public PragmaEnumerator(ReadOnlySpan<char> text)
        {
            this.text = text;
        }

        public readonly Pragma Current => this.current;

        public readonly PragmaEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var remaining = this.text;
            if (remaining.IsEmpty)
                return false;

            var start = Pragma.FindMarkerIndex(remaining);
            if (start < 0)
            {
                this.text = default;
                return false;
            }

            remaining = remaining[(start + Pragma.Marker.Length)..];
            var mid = remaining.IndexOf('\n');
            if (mid > 0)
            {
                var sep = remaining.IndexOf(' ');
                if (sep > 0 && sep < mid)
                {
                    var end = Pragma.FindMarkerIndex(remaining);
                    if (end < 0)
                    {
                        end = remaining.Length;
                    }

                    var name = remaining[..sep].Trim();
                    var meta = remaining[(sep + 1)..mid].Trim();
                    var data = remaining[mid..end].Trim();
                    this.current = new Pragma(name, meta, data);

                    this.text = remaining[end..];
                    return true;
                }
            }

            this.text = remaining;
            return false;
        }
    }
}

static class Globbing
{
    private static readonly SearchValues<char> Wildcards = SearchValues.Create("*?");

    public static bool HasWildcard([NotNullWhen(true)] this string? name) => name.AsSpan().HasWildcard();

    public static bool HasWildcard(this ReadOnlySpan<char> name) => name.Length > 0 && name.IndexOfAny(Wildcards) >= 0;

    public static int CommonPrefixLength(this string[] names)
    {
        if (names.Length < 2)
            return 0;

        var i = 1;
        var name = names[0].AsSpan();
        var commonPrefixLength = name.Length;

        while (i < names.Length)
        {
            commonPrefixLength = Math.Min(commonPrefixLength, name.CommonPrefixLength(names[i++]));
            if (commonPrefixLength == 0)
                return 0;
        }

        return commonPrefixLength;
    }
}