namespace Spryer;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class Globbing
{
	private static readonly SearchValues<char> Wildcards = SearchValues.Create("*?");

	public static bool HasWildcards(this string name) => name.AsSpan().HasWildcards();

	public static bool HasWildcards(this ReadOnlySpan<char> name) => name.Length > 0 && name.IndexOfAny(Wildcards) >= 0;

	public static bool Matches(this string pattern, ReadOnlySpan<char> name) => pattern.AsSpan().Matches(name);

	public static bool Matches(this ReadOnlySpan<char> pattern, ReadOnlySpan<char> name)
	{
		var nameLen = name.Length;
		var patternLen = pattern.Length;
		ref var nameRef = ref MemoryMarshal.GetReference(name);
		ref var patternRef = ref MemoryMarshal.GetReference(pattern);

		var starLen = 0;
		while (patternLen > 0)
		{
			if (patternRef == '*')
			{
				while (patternLen > 0)
				{
					if (patternRef != '*')
					{
						break;
					}
					else
					{
						patternRef = ref Unsafe.Add(ref patternRef, 1);
						patternLen--;
					}
				}

				if (patternLen == 0)
					return true;

				starLen = patternLen;

				if (patternRef == '?')
					continue;

				while (nameLen > 0)
				{
					if (patternLen > 0 && char.ToUpperInvariant(nameRef) == char.ToUpperInvariant(patternRef))
					{
						break;
					}
					else
					{
						nameRef = ref Unsafe.Add(ref nameRef, 1);
						nameLen--;
					}
				}
			}
			else
			{
				if (nameLen == 0)
					return false;

				if (patternRef != '?' && char.ToUpperInvariant(nameRef) != char.ToUpperInvariant(patternRef))
				{
					if (starLen > 0)
					{
						patternRef = ref Unsafe.Add(ref patternRef, patternLen - starLen);
						patternLen = starLen;

						nameRef = ref Unsafe.Add(ref nameRef, 1);
						nameLen--;

						continue;
					}

					return false;
				}

				nameRef = ref Unsafe.Add(ref nameRef, 1);
				nameLen--;

				patternRef = ref Unsafe.Add(ref patternRef, 1);
				patternLen--;
			}
		}

		return patternLen == 0 && (nameLen == 0 || pattern[^1] == '*');
	}
}