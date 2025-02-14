namespace Spryer;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ScriptMap = System.Collections.Frozen.FrozenDictionary<string, string>;

/// <summary>
/// Represents a collection of SQL scripts loaded from an external source.
/// </summary>
[DebuggerDisplay("{Source}/{Version}: {Count}")]
public class DbScriptMap
{
    private const string ScriptsFileName = "Scripts";
    private const string ScriptsFileExt = ".sql";

    /// <summary>
    /// An empty instance of <see cref="DbScriptMap"/>.
    /// </summary>
    public static readonly DbScriptMap Empty = new(string.Empty, new(), ScriptMap.Empty);

    private readonly string source;
    private readonly Version version;
    private readonly ScriptMap scripts;

    private DbScriptMap(string source, Version version, ScriptMap scripts)
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
        get => scripts[name];
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
        if (TryLoad(fileName, assembly: default, out var scriptMap))
            return scriptMap;

        if (TryLoad(fileName, Assembly.GetEntryAssembly(), out scriptMap))
            return scriptMap;

        if (TryLoad(fileName, Assembly.GetCallingAssembly(), out scriptMap))
            return scriptMap;

        return Empty;
    }

    private static bool TryLoad(string? fileName, Assembly? assembly, out DbScriptMap scriptMap)
    {
        foreach (var scriptFileName in EnumerateFileNames(assembly, fileName))
        {
            if (string.IsNullOrWhiteSpace(scriptFileName))
                continue;

            // scripts are loaded from the external files or the assembly resources

            foreach (var scriptFilePath in EnumerateFilePaths(assembly, scriptFileName))
            {
                if (string.IsNullOrWhiteSpace(scriptFilePath))
                    continue;


                Debug.WriteLine($"Spryer: looking for '{scriptFilePath}'");

                if (File.Exists(scriptFilePath) && TryLoad(scriptFilePath, File.OpenRead(scriptFilePath), out scriptMap))
                {
                    return true;
                }
            }

            if (assembly is not null &&
                Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(scriptFileName, StringComparison.OrdinalIgnoreCase)) is string resourceName &&
                assembly.GetManifestResourceStream(resourceName) is Stream resourceStream && TryLoad(resourceName, resourceStream, out scriptMap))
            {
                return true;
            }
        }

        scriptMap = Empty;
        return false;
    }

    private static bool TryLoad(string scriptSource, Stream scriptStream, out DbScriptMap scriptMap)
    {
        using var reader = new StreamReader(scriptStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var scriptText = reader.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(scriptText))
        {
            var scripts = ParseScripts(scriptText, out var version);
            scriptMap = new(scriptSource, version, scripts);

            Debug.WriteLine($"Spryer: {scripts.Count} scripts found in '{scriptSource}'");
            return true;
        }

        scriptMap = Empty;
        return false;
    }

    private static IEnumerable<string?> EnumerateFilePaths(Assembly? assembly, string fileName)
    {
        if (assembly is not null)
        {
            if (Path.GetDirectoryName(assembly.Location) is { Length: > 0 } assemblyDir)
                yield return Path.Combine(assemblyDir, fileName);

            yield return Path.Combine(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), assembly.GetName().Name!),
                fileName);
        }
        else
        {
            if (Path.GetDirectoryName(Environment.ProcessPath) is { Length: > 0 } processDir)
                yield return Path.Combine(processDir, fileName);

            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) is { Length: > 0 } processName)
            {
                yield return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), processName),
                    fileName);
            }
        }
    }

    private static IEnumerable<string?> EnumerateFileNames(Assembly? assembly, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
            yield return fileName;

        yield return Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ScriptsFileExt);
        yield return Path.GetFileNameWithoutExtension(Environment.ProcessPath) + "." + ScriptsFileName + ScriptsFileExt;

        if (assembly is not null)
        {
            yield return assembly.GetName().Name + ScriptsFileExt;
            yield return assembly.GetName().Name + "." + ScriptsFileName + ScriptsFileExt;
            yield return Path.ChangeExtension(Path.GetFileName(assembly.Location), ScriptsFileExt);
        }

        yield return ScriptsFileName + ScriptsFileExt;
    }

    private static ScriptMap ParseScripts(ReadOnlySpan<char> text, out Version version)
    {
        // any sql statements before the first pragma are ignored
        var start = text.IndexOf(Pragma.Prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            version = Empty.Version;
            return ScriptMap.Empty;
        }

        text = text[start..];

        var foundVersion = default(Version);
        var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pragma in EnumeratePragmas(text))
        {
            if (pragma.Name.Equals(Pragma.Script, StringComparison.OrdinalIgnoreCase))
            {
                ref var script = ref CollectionsMarshal.GetValueRefOrAddDefault(scripts, pragma.Meta.ToString(), out _);
                script = pragma.Data.ToString();
            }
            else if (pragma.Name.Equals(Pragma.Version, StringComparison.OrdinalIgnoreCase))
            {
                Version.TryParse(pragma.Meta, out foundVersion);
            }
        }

        version = foundVersion ?? Empty.Version;
        return scripts.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static PragmaEnumerator EnumeratePragmas(ReadOnlySpan<char> text) => new(text);

    [DebuggerDisplay("@{Name,nq} {Meta,nq}")]
    private readonly ref struct Pragma
    {
        public const string Prefix = "--@";
        public const string Suffix = "\n--@";
        public const string AltSuffix = "\r--@";

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

            var start = remaining.IndexOf(Pragma.Prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                this.text = default;
                return false;
            }

            remaining = remaining[(start + Pragma.Prefix.Length)..];
            var mid = remaining.IndexOf('\n');
            if (mid > 0)
            {
                var sep = remaining.IndexOf(' ');
                if (sep > 0 && sep < mid)
                {
                    var end = remaining.IndexOf(Pragma.Suffix, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        end = remaining.IndexOf(Pragma.AltSuffix, StringComparison.Ordinal);
                        if (end < 0)
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