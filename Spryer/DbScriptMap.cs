#if NET6_0_OR_GREATER
namespace Spryer;
#else
namespace Spryer.Scripting;
#endif

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FrozenScripts = System.Collections.Frozen.FrozenDictionary<string, DbScript>;
using MutableScripts = System.Collections.Generic.Dictionary<string, DbScript>;

/// <summary>
/// Represents a collection of SQL scripts loaded from an external source.
/// </summary>
[DebuggerDisplay("{Source}/{Version}: {Count}")]
public sealed class DbScriptMap
{
    private const string ScriptsFileName = "Scripts";
    private const string ScriptsFileExt = ".sql";

    private readonly FrozenScripts scripts;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbScriptMap"/> class.
    /// </summary>
    /// <param name="scripts">The collection of scripts</param>
    /// <param name="source">The source for scripts in the collection.</param>
    /// <param name="version">The version of the collection.</param>
    private DbScriptMap(FrozenScripts scripts, string source, Version version)
    {
        this.scripts = scripts;
        this.Source = source;
        this.Version = version;
        this.Pragmas = Array.Empty<CustomPragma>().ToLookup(p => p.Name, p => p.Meta, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// An empty instance of <see cref="DbScriptMap"/>.
    /// </summary>
    public static readonly DbScriptMap Empty = new(FrozenScripts.Empty, string.Empty, new());

    /// <summary>
    /// Gets the SQL script with the specified name.
    /// </summary>
    /// <param name="name">A script name.</param>
    /// <returns>A SQL script with the specified name or <c>string.Empty</c> if no such script is found.</returns>
    public string this[string name] => this.scripts.GetValueOrDefault(name, DbScript.Empty).Text;

    /// <summary>
    /// Finds the <see cref="DbScript"/> object with the specified name.
    /// </summary>
    /// <param name="name">A script name.</param>
    /// <returns>A <see cref="DbScript"/> object with the specified name or <c>null</c> if no such script is found.</returns>
    internal DbScript? Find(string name) => this.scripts.GetValueOrDefault(name);

    /// <summary>
    /// Gets the sources for scripts in the collection separated by a new line.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the version of the collection.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets the number of scripts in the collection.
    /// </summary>
    public int Count => this.scripts.Count;

    internal IEnumerable<DbScript> Enumerate() => this.scripts.Values;

    internal ILookup<string, string> Pragmas { get; init; }

    /// <summary>
    /// Loads a collection of SQL scripts from an external source.
    /// </summary>
    /// <param name="fileName">A script file name.</param>
    /// <returns>A collection of SQL scripts.</returns>
    public static DbScriptMap Load(string? fileName = null) =>
        LoadInternal(Assembly.GetCallingAssembly(), fileName, expectedVersion: null);

    /// <summary>
    /// Loads a collection of SQL scripts from an external source with a specified file name
    /// and an expected minimal version.
    /// </summary>
    /// <param name="fileName">The script file name.</param>
    /// <param name="expectedVersion">The script file expected minimal version.</param>
    /// <returns>A collection of SQL scripts.</returns>
    public static DbScriptMap Load(string fileName, Version expectedVersion) =>
        LoadInternal(Assembly.GetCallingAssembly(), fileName, expectedVersion);

    /// <summary>
    /// Loads a collection of SQL scripts from an external source with an expected minimal version.
    /// </summary>
    /// <param name="expectedVersion">A script file expected minimal version.</param>
    /// <returns>A collection of SQL scripts.</returns>
    public static DbScriptMap Load(Version expectedVersion) =>
        LoadInternal(Assembly.GetCallingAssembly(), fileName: null, expectedVersion);

    private static DbScriptMap LoadInternal(Assembly callingAssembly, string? fileName, Version? expectedVersion)
    {
        var loader = new Loader
        {
            FileName = fileName,
            Assembly = callingAssembly
        };
        loader.TryLoadScripts();

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != callingAssembly)
        {
            loader.Assembly = entryAssembly;
            loader.TryLoadScripts();
        }

        loader.Assembly = null;
        loader.TryLoadScripts();

        var scriptMap = loader.GetScriptMap();

        if (expectedVersion is not null && scriptMap.Version < expectedVersion)
            throw new ScriptMapVersionMismatchException();

        return scriptMap;
    }

    internal sealed record CustomPragma(string Name, string Meta)
    {
        public CustomPragma(in Pragma pragma)
            : this(pragma.Name.ToString(), pragma.Meta.ToString()) { }
    }

    /// <summary>
    /// Represents a loader for the script collection.
    /// </summary>
    public class Loader
    {
        private readonly StringBuilder source;
        private readonly MutableScripts scripts;
        private readonly List<CustomPragma> pragmas;
        private Version version;

        /// <summary>
        /// Creates a new <see cref="Loader"/> instance.
        /// </summary>
        public Loader()
        {
            this.source = new();
            this.scripts = new(StringComparer.OrdinalIgnoreCase);
            this.pragmas = [];
            this.version = Empty.Version;
        }

        /// <summary>
        /// Gets or sets the desired file name for the script collection.
        /// </summary>
        public string? FileName { get; init; }

        /// <summary>
        /// Gets or sets the assembly to look for the script collection.
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the loader should collect custom pragmas.
        /// </summary>
#if NETSTANDARD2_0
        public
#else
        internal
#endif
        bool CollectsPragmas
        { get; init; }

        /// <summary>
        /// Gets the loaded script collection.
        /// </summary>
        public DbScriptMap GetScriptMap()
        {
            if (this.scripts.Count > 0)
            {
                return new(this.scripts.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), this.source.ToString(), this.version)
                {
                    Pragmas = this.CollectsPragmas ? this.pragmas.ToLookup(p => p.Name, p => p.Meta, StringComparer.OrdinalIgnoreCase) : Empty.Pragmas
                };
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
                var scriptCount = ParseScripts(scriptText.AsSpan());
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
            using var reader = new StreamReader(scriptStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
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
#if NET6_0_OR_GREATER
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
#endif
        }

        private IEnumerable<string> EnumerateFilePaths(string scriptFileName)
        {
#if NETSTANDARD2_0
            if (Path.IsPathRooted(scriptFileName))
            {
                yield return scriptFileName;
            }
#endif

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
#if NET6_0_OR_GREATER
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
#endif
        }

        private IEnumerable<string?> EnumerateFileNames()
        {
            if (!string.IsNullOrWhiteSpace(this.FileName) && !this.FileName.HasWildcard())
            {
                yield return this.FileName;
            }
            else
            {
#if NET6_0_OR_GREATER
                yield return Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ScriptsFileExt);
                yield return Path.GetFileNameWithoutExtension(Environment.ProcessPath) + "." + ScriptsFileName + ScriptsFileExt;
#endif

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
            var start = Pragma.FindMarkerIndex(text, out _);
            if (start < 0)
            {
                return 0;
            }

            text = text[start..];

            var count = 0;
            foreach (var pragma in Pragma.Enumerate(text))
            {
                if (DbScript.TryParse(pragma, out var parsed))
                {
#if NET6_0_OR_GREATER
                    ref var script = ref CollectionsMarshal.GetValueRefOrAddDefault(this.scripts, parsed.Name, out _);
                    script = parsed;
#else
                    this.scripts[parsed.Name] = parsed;
#endif
                    ++count;
                }
                else if (pragma.Name.Equals(Pragma.Version, StringComparison.OrdinalIgnoreCase))
                {
                    if (
#if NET6_0_OR_GREATER
                        Version.TryParse(pragma.Meta, out var foundVersion)
#else
                        Version.TryParse(pragma.Meta.ToString(), out var foundVersion)
#endif
                        && foundVersion > this.version
                        )
                    {
                        this.version = foundVersion;
                    }
                }
                else
                {
                    this.pragmas.Add(new(pragma));
                }
            }

            return count;
        }
    }
}

abstract record DbScriptDataType(DbType Type)
{
    public int Size { get; init; }

    public string? CustomType { get; init; }

    protected static readonly FrozenDictionary<string, DbType> DbTypeMap =
        (new KeyValuePair<string, DbType>[]
        {
            new("bigint", DbType.Int64),
            new("binary", DbType.Binary),
            new("bit", DbType.Boolean),
            new("char", DbType.AnsiStringFixedLength),
            new("date", DbType.Date),
            new("datetime", DbType.DateTime),
            new("datetime2", DbType.DateTime2),
            new("datetimeoffset", DbType.DateTimeOffset),
            new("decimal", DbType.Decimal),
            new("float", DbType.Double),
            new("image", DbType.Binary),
            new("int", DbType.Int32),
            new("money", DbType.Currency),
            new("nchar", DbType.StringFixedLength),
            new("ntext", DbType.String),
            new("numeric", DbType.Decimal),
            new("nvarchar", DbType.String),
            new("real", DbType.Single),
            new("rowversion", DbType.Binary),
            new("smalldatetime", DbType.DateTime),
            new("smallint", DbType.Int16),
            new("smallmoney", DbType.Currency),
            new("sql_variant", DbType.Object),
            new("text", DbType.AnsiString),
            new("time", DbType.Time),
            new("timestamp", DbType.Binary),
            new("tinyint", DbType.Byte),
            new("uniqueidentifier", DbType.Guid),
            new("varbinary", DbType.Binary),
            new("varchar", DbType.AnsiString),
            new("xml", DbType.Xml),
         }).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}

[DebuggerDisplay("@{Name,nq} {Type}({Size})")]
record DbScriptParameter(string Name, DbType Type) : DbScriptDataType(Type)
{
    internal static bool TryParse(ReadOnlySpan<char> span, [NotNullWhen(true)] out DbScriptParameter? parameter)
    {
        var name = span;
        var type = DbType.String;
        var customType = default(string);
        var size = -1;

        var mid = span.IndexOf(' ');
        if (mid > 0)
        {
            name = span[..mid];
            var typeName = span[mid..].TrimStart();

            var sep = typeName.IndexOfUnclosed('(', '[', ']');
            if (sep > 0)
            {
                var typeSize = typeName[(sep + 1)..].Trim().TrimEnd(')').Trim();
                if (
#if NET6_0_OR_GREATER
                    int.TryParse(typeSize, out var parsedSize)
#else
                    int.TryParse(typeSize.ToString(), out var parsedSize)
#endif
                    )
                {
                    size = parsedSize;
                }

                typeName = typeName[..sep].TrimEnd();
            }

            if (typeName.Length > 0)
            {
                if (typeName[0] != '[' && typeName[^1] != ']' &&
                    DbTypeMap.TryGetValue(typeName.ToString(), out var foundType))
                {
                    type = foundType;
                }
                else
                {
                    type = DbType.Object;
                    customType = typeName.TrimStart('[').TrimEnd(']').Trim().ToString();
                }
            }
        }

        if (name.Length > 1)
        {
            parameter = new(name.TrimStart('@').ToString(), type)
            {
                Size = size,
                CustomType = customType,
            };
            return true;
        }

        parameter = null;
        return false;
    }
}

record DbScriptReturnType(DbType Type) : DbScriptDataType(Type)
{
    public static readonly DbScriptReturnType Implicit = new(DbType.Object);

    public static bool TryParse(ReadOnlySpan<char> span, [NotNullWhen(true)] out DbScriptReturnType? returnType)
    {
        if (span.Length > 0)
        {
            var type = DbType.Object;
            var customType = default(string);
            var size = -1;

            var typeName = span;

            var sep = typeName.IndexOfUnclosed('(', '[', ']');
            if (sep > 0)
            {
                var typeSize = typeName[(sep + 1)..].Trim().TrimEnd(')').Trim();
                if (
#if NET6_0_OR_GREATER
                    int.TryParse(typeSize, out var parsedSize)
#else
                    int.TryParse(typeSize.ToString(), out var parsedSize)
#endif
                    )
                {
                    size = parsedSize;
                }

                typeName = typeName[..sep].TrimEnd();
            }

            if (typeName.Length > 0)
            {
                if (typeName[0] != '[' && typeName[^1] != ']' &&
                    DbTypeMap.TryGetValue(typeName.ToString(), out var foundType))
                {
                    type = foundType;
                }
                else
                {
                    type = DbType.Object;
                    customType = typeName.TrimStart('[').TrimEnd(']').Trim().ToString();
                }
            }

            if (type != DbType.Object || !string.IsNullOrWhiteSpace(customType))
            {
                returnType = new(type)
                {
                    Size = size,
                    CustomType = customType,
                };
                return true;
            }
        }

        returnType = default;
        return false;
    }
}

/// <summary>
/// Represents a kind of Dapper extension method to use with the script.
/// </summary>
public enum DbScriptType
{
    /// <summary>
    /// A generic script, any method.
    /// </summary>
    Generic,
    /// <summary>
    /// Execute() method.
    /// </summary>
    Execute,
    /// <summary>
    /// ExecuteReader() method.
    /// </summary>
    ExecuteReader,
    /// <summary>
    /// ExecuteScalar() method.
    /// </summary>
    ExecuteScalar,
    /// <summary>
    /// Query() method.
    /// </summary>
    Query,
    /// <summary>
    /// QueryFirst() method.
    /// </summary>
    QueryFirst,
    /// <summary>
    /// QueryFirstOrDefault() method.
    /// </summary>
    QueryFirstOrDefault,
    /// <summary>
    /// QuerySingle() method.
    /// </summary>
    QuerySingle,
    /// <summary>
    /// QuerySingleOrDefault() method.
    /// </summary>
    QuerySingleOrDefault,
    /// <summary>
    /// QueryMultiple() method.
    /// </summary>
    QueryMultiple,
    /// <summary>
    /// QueryUnbuffered() method.
    /// </summary>
    QueryUnbuffered,
    /// <summary>
    /// QueryText() method (Spryer).
    /// </summary>
    QueryText,
    /// <summary>
    /// QueryJson() method (Spryer).
    /// </summary>
    QueryJson,
}

record DbScript(string Name, string Text)
{
    public static readonly DbScript Empty = new(string.Empty, string.Empty);

    public DbScriptType Type { get; init; } = DbScriptType.Generic;
    public DbScriptParameter[] Parameters { get; init; } = [];
    public DbScriptReturnType ReturnType { get; init; } = DbScriptReturnType.Implicit;
    public bool HasReturnType => this.ReturnType != DbScriptReturnType.Implicit;

    internal static bool TryParse(in Pragma pragma, [NotNullWhen(true)] out DbScript? script)
    {
        if (!pragma.IsScript)
        {
            script = null;
            return false;
        }

        var scriptName = string.Empty;
        var parameters = new List<DbScriptParameter>();
        var returnType = DbScriptReturnType.Implicit;
        foreach (var token in MetaToken.Enumerate(pragma.Meta))
        {
            if (token.Type == MetaTokenType.Parameter && DbScriptParameter.TryParse(token.Span, out var parameter))
            {
                parameters.Add(parameter);
            }
            else if (token.Type == MetaTokenType.ScriptName)
            {
                scriptName = token.Span.ToString();
            }
            else if (token.Type == MetaTokenType.ReturnType && DbScriptReturnType.TryParse(token.Span, out var parsedReturnType))
            {
                returnType = parsedReturnType;
            }
        }

        if (string.IsNullOrWhiteSpace(scriptName))
        {
            script = null;
            return false;
        }

        script = new(scriptName, pragma.Data.ToString())
        {
            Type = DetectScriptType(pragma),
            Parameters = parameters.ToArray(),
            ReturnType = returnType,
        };
        return true;
    }

    private static DbScriptType DetectScriptType(in Pragma pragma)
    {
        var scriptType = knownScriptTypes.GetValueOrDefault(pragma.Name.ToString());
        if (scriptType == DbScriptType.Generic)
        {
            scriptType = pragma.Data.StartsWith("select ", StringComparison.OrdinalIgnoreCase) ||
                pragma.Data.StartsWith("with ", StringComparison.OrdinalIgnoreCase) ?
                DbScriptType.Query : DbScriptType.Execute;
        }

        return scriptType;
    }

    private enum MetaTokenType
    {
        Unknown,
        ScriptName,
        Parameter,
        ReturnType,
    }

    [DebuggerDisplay("{Type}: {Span}")]
    private readonly ref struct MetaToken
    {
        public MetaToken(ReadOnlySpan<char> span, MetaTokenType type)
        {
            this.Span = span;
            this.Type = type;
        }

        public ReadOnlySpan<char> Span { get; }
        public MetaTokenType Type { get; }

        public static MetaTokenEnumerator Enumerate(ReadOnlySpan<char> meta) => new(meta);
    }

    private ref struct MetaTokenEnumerator
    {
        private static readonly SearchValues<char> NameSeparators = SearchValues.Create(" (:");
        private static readonly SearchValues<char> ParamSeparators = SearchValues.Create(",)");

        private ReadOnlySpan<char> meta;
        private MetaTokenType type;
        private MetaToken current;

        public MetaTokenEnumerator(ReadOnlySpan<char> meta)
        {
            this.meta = meta;
            this.type = MetaTokenType.Unknown;
        }

        public readonly MetaToken Current => this.current;

        public readonly MetaTokenEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var remaining = this.meta;
            if (remaining.IsEmpty)
                return false;

            if (this.type != MetaTokenType.Parameter)
            {
                ++this.type;
            }

            if (this.type == MetaTokenType.ScriptName)
            {
                var span = remaining;
                var end = remaining.IndexOfAnyUnquoted(NameSeparators, '"');
                if (end > 0)
                {
                    span = remaining[0] == '"' && end > 2 ? remaining[1..(end - 1)] : remaining[..end];
                    this.meta = remaining[end..].TrimStart();
                }
                else
                {
                    this.meta = default;
                }

                this.current = new(span, this.type);
                return true;
            }

            if (this.type == MetaTokenType.Parameter)
            {
                if (remaining[0] == '(')
                    remaining = remaining[1..];

                var end = remaining.IndexOfAnyUnclosed(ParamSeparators, '(', ')');
                if (end > 0)
                {
                    var span = remaining[..end].Trim();

                    this.current = new(span, this.type);
                    this.meta = remaining[end..].TrimStart(',');
                    return true;
                }
                else
                {
                    ++this.type;
                }
            }

            if (this.type == MetaTokenType.ReturnType)
            {
                var start = remaining.IndexOf(':');
                if (start >= 0 && start < (remaining.Length - 1))
                {
                    var span = remaining[(start + 1)..].Trim();
                    if (span.Length > 0)
                    {
                        this.current = new(span, this.type);
                        this.meta = default;
                        return true;
                    }
                }
            }

            this.meta = default;
            return false;
        }
    }

    private static readonly FrozenDictionary<string, DbScriptType> knownScriptTypes =
        (new KeyValuePair<string, DbScriptType>[]
        {
            new("script", DbScriptType.Generic),
            new("execute", DbScriptType.Execute),
            new("execute-reader", DbScriptType.ExecuteReader),
            new("execute-scalar", DbScriptType.ExecuteScalar),
            new("query", DbScriptType.Query),
            new("query-first", DbScriptType.QueryFirst),
            new("query-first-default", DbScriptType.QueryFirstOrDefault),
            new("query-single", DbScriptType.QuerySingle),
            new("query-single-default", DbScriptType.QuerySingleOrDefault),
            new("query-multiple", DbScriptType.QueryMultiple),
            new("query-unbuffered", DbScriptType.QueryUnbuffered),
            new("query-text", DbScriptType.QueryText),
            new("query-json", DbScriptType.QueryJson),
        }).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}

[DebuggerDisplay("@{Name,nq} {Meta,nq}")]
readonly ref struct Pragma
{
    public const string Marker = "--@";
    public const string AltMarker = "/*@";
    public const string AltMarkerEnd = "@*/";
    public static readonly SearchValues<char> NewLineChars = SearchValues.Create("\r\n");

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

    public bool IsScript
    {
        get =>
            this.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            this.Name.StartsWith("query", StringComparison.OrdinalIgnoreCase) ||
            this.Name.StartsWith("execute", StringComparison.OrdinalIgnoreCase);
    }

    public string GetMetaName()
    {
        var meta = this.Meta;

        var end = meta.IndexOfUnquoted(' ', '"');
        if (end > 0)
            meta = meta[0] == '"' && end > 2 ? meta[1..(end - 1)] : meta[..end];

        return meta.ToString();
    }

    public static PragmaEnumerator Enumerate(ReadOnlySpan<char> text) => new(text);

    public static int FindMarkerIndex(ReadOnlySpan<char> text, out bool isMultiline)
    {
        isMultiline = false;
        var offset = 0;
        var index = -1;

        var span = text;
        while (span.Length > 0)
        {
            index = IndexOfMarker(span, out isMultiline);
            if (index == 0)
                return offset;
            if (index < 0)
                return -1;

            if (index > 0)
            {
                if (IndexIsCommentedOut(index, span, out var commentRange))
                {
                    if (commentRange.Equals(Range.All))
                        return -1;

                    offset += commentRange.End.GetOffset(span.Length) - 1;
                    span = span[commentRange.End..];

                    continue;
                }

                if (NewLineChars.Contains(span[index - 1]))
                {
                    break;
                }

                offset += index;
                span = span[(index + 1)..];
            }
        }

        return offset + index;
    }

    internal static Range FindNameRange(ReadOnlySpan<char> span)
    {
        var start = span.IndexOfAnyExcept(' ');
        if (start >= 0)
        {
            var end = span[start..].IndexOf(' ');
            if (end > 0)
            {
                return start..(start + end);
            }
        }

        return default;
    }

    private static int IndexOfMarker(ReadOnlySpan<char> span, out bool altMarker)
    {
        var offset = 0;

        while (span.Length > 0)
        {
            var index = span.IndexOf('@');
            if (index < 0) break;
            if (index < (Marker.Length - 1))
            {
                ++index;
                span = span[index..];
                offset += index;
                continue;
            }

            index -= Marker.Length - 1;
            span = span[index..];
            offset += index;

            if (span.StartsWith(Marker, StringComparison.Ordinal))
            {
                altMarker = false;
                return offset;
            }
            else if (span.StartsWith(AltMarker, StringComparison.Ordinal))
            {
                altMarker = true;
                return offset;
            }

            span = span[Marker.Length..];
            offset += Marker.Length;
        }

        altMarker = default;
        return -1;
    }

    private static bool IndexIsCommentedOut(int index, ReadOnlySpan<char> span, out Range commentRange)
    {
        var start = span.IndexOf("/*", StringComparison.Ordinal);
        if (start < 0 || start >= index)
        {
            // equality to index means alt marker, nested comments are not allowed anyway
            commentRange = default;
            return false;
        }

        var offset = 0;
        var comment = 0;
        while (span.Length > 0)
        {
            var end = span.IndexOf('*');
            if (end <= 0)
                break;

            if (span[end - 1] == '/')
            {
                // comment start
                ++comment;
                ++end;
            }
            else if (span.Length - end > 1 && span[end + 1] == '/')
            {
                // comment end
                --comment;
                end += 2;
            }
            else
            {
                ++end;
            }

            if (comment == 0)
            {
                commentRange = start..(offset + end);
                return true;
            }
            else if (end == span.Length)
            {
                break;
            }
            else
            {
                offset += end - 1;
                span = span[end..];
            }
        }

        commentRange = Range.All;
        return true;
    }
}

ref struct PragmaEnumerator
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
        while (remaining.Length > 0)
        {
            var start = Pragma.FindMarkerIndex(remaining, out var alt);
            if (start < 0)
                break;

            remaining = remaining[(start + (alt ? Pragma.AltMarker.Length : Pragma.Marker.Length))..];
            var mid = alt ? remaining.IndexOf(Pragma.AltMarkerEnd, StringComparison.Ordinal) : remaining.IndexOfAny(Pragma.NewLineChars);
            if (mid > 0)
            {
                var nr = Pragma.FindNameRange(remaining);
                if (!nr.Equals(default) && nr.End.GetOffset(remaining.Length) < mid)
                {
                    var mr = nr.End..mid;
                    var end = Pragma.FindMarkerIndex(remaining, out _);
                    if (end < 0) end = remaining.Length;
                    var dr = alt ? (mid + Pragma.AltMarkerEnd.Length)..end : mid..end;

                    var name = remaining[nr].Trim();
                    var meta = remaining[mr].Trim();
                    var data = remaining[dr].Trim();
                    this.current = new Pragma(name, meta, data);

                    this.text = remaining[end..];
                    return true;
                }
            }
        }

        this.text = default;
        return false;
    }
}

/// <summary>
/// Represents a version mismatch error between the expected and actual version of the script map.
/// </summary>
[Serializable]
public class ScriptMapVersionMismatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptMapVersionMismatchException"/> class.
    /// </summary>
    public ScriptMapVersionMismatchException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptMapVersionMismatchException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ScriptMapVersionMismatchException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptMapVersionMismatchException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    ///  or a <c>null</c> reference if no inner exception is specified.</param>
    public ScriptMapVersionMismatchException(string? message, Exception? innerException) : base(message, innerException)
    {
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

        var first = names[0].AsSpan();
        var minLength = first.Length;

        for (var i = 1; i != names.Length; ++i)
        {
            minLength = Math.Min(minLength, names[i].Length);
        }

        if (minLength == 0) return 0;

        var commonLength = 0;
        for (var i = 0; i != minLength; ++i)
        {
            var currentChar = first[i];
            var allMatch = true;
            for (var j = 1; j != names.Length; ++j)
            {
                if (names[j][i] != currentChar)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                commonLength++;
            }
            else
            {
                break;
            }
        }

        return commonLength;
    }

    public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, char quote)
    {
        var len = span.Length;
        if (len == 0) return -1;

        var quoted = false;
        ref var src = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            quoted ^= src == quote;
            if (!quoted && src == value)
            {
                return span.Length - len;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return -1;
    }

    public static int IndexOfAnyUnquoted(this ReadOnlySpan<char> span, SearchValues<char> values, char quote)
    {
        var len = span.Length;
        if (len == 0) return -1;

        var quoted = false;
        ref var src = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            quoted ^= src == quote;
            if (!quoted && values.Contains(src))
            {
                return span.Length - len;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return -1;
    }

    public static int IndexOfUnclosed(this ReadOnlySpan<char> span, char value, char opener, char closer)
    {
        var len = span.Length;
        if (len == 0) return -1;

        var enclosed = 0;
        ref var src = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            if (src == opener)
            {
                ++enclosed;
            }
            else if (src == closer && enclosed > 0)
            {
                --enclosed;
            }
            else if (enclosed == 0 && src == value)
            {
                return span.Length - len;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return -1;
    }

    public static int IndexOfAnyUnclosed(this ReadOnlySpan<char> span, SearchValues<char> values, char opener, char closer)
    {
        var len = span.Length;
        if (len == 0) return -1;

        var enclosed = 0;
        ref var src = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            if (src == opener)
            {
                ++enclosed;
            }
            else if (src == closer && enclosed > 0)
            {
                --enclosed;
            }
            else if (enclosed == 0 && values.Contains(src))
            {
                return span.Length - len;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return -1;
    }
}