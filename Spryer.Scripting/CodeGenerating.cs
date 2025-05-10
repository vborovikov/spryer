namespace Spryer.Scripting;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface ICodeGenerator
{
    void Generate(CodeBuilder code);
}

static class CodeGenerating
{
    private static readonly SearchValues<char> VarTrimChars = SearchValues.Create("-.");

    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Normalize(input.ToCharArray(), VarTrimChars).ToString();
    }

    public static string ToCamelCase(this string input, bool skipKeywordCheck = false)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var normalized = Normalize(input.ToCharArray(), VarTrimChars, camelCase: true).ToString();
        
        if (!skipKeywordCheck && ReservedKeywords.Contains(normalized))
            return $"@{normalized}";

        if (ReservedNames.Contains(normalized))
            return $"_{normalized}";

        return normalized;
    }

    private static ReadOnlySpan<char> Normalize(Span<char> span, SearchValues<char> trimChars, bool camelCase = false)
    {
        var len = span.Length;
        if (len == 0) return [];

        var pos = 0;
        var space = !camelCase;
        ref char src = ref MemoryMarshal.GetReference(span);
        ref char dst = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            if (char.IsWhiteSpace(src) || trimChars.Contains(src))
            {
                space = true;
            }
            else
            {
                dst = space ? char.ToUpperInvariant(src) : camelCase ? char.ToLowerInvariant(src) : src;
                dst = ref Unsafe.Add(ref dst, 1);
                ++pos;

                space = false;
                camelCase = false;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return span[..pos];
    }

    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "add", "alias", "allows", "and", "args", "as", "ascending", "async", "await", "base",
        "bool", "break", "by", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "equals",
        "event", "explicit", "extern", "false", "field", "file", "finally", "fixed", "float", "for", "foreach",
        "from", "get", "global", "goto", "group", "if", "implicit", "in", "init", "int", "interface", "internal",
        "into", "is", "join", "let", "lock", "long", "managed", "nameof", "namespace", "new", "nint", "not",
        "notnull", "nuint", "null", "object", "on", "operator", "or", "orderby", "out", "override", "params",
        "partial", "private", "protected", "public", "readonly", "record", "ref", "remove", "required", "return",
        "sbyte", "scoped", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unmanaged",
        "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "when", "where", "while",
        "with", "yield",
    };

    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "cancellationToken", "commandTimeout", "commandType", "connection", "database", "exception", "returnValue", "transaction"
    };
}
