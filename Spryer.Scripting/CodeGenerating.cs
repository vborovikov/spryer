namespace Spryer.Scripting;

using System;
using System.Buffers;
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

    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Normalize(input.ToCharArray(), VarTrimChars, camelCase: true).ToString();
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
}
