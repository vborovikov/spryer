namespace Spryer;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dapper;

/// <summary>
/// Provides the enumeration type information.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
internal static class EnumInfo<T>
    where T : struct, Enum
{
    public const char DefaultValueSeparator = ',';
    private static readonly FrozenDictionary<T, string> names;

    /// <summary>
    /// Initializes static members of the <see cref="EnumInfo{T}"/> class.
    /// </summary>
    static EnumInfo()
    {
        HasFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
        names = GetNames();

        MaxLength = HasFlags ?
            names.Values.Sum(x => x.Length) + names.Count - 1 :
            names.Values.Max(x => x.Length);

        static FrozenDictionary<T, string> GetNames()
        {
            var names = new Dictionary<T, string>();

            var enumValues = Enum.GetValues<T>();
            var enumNames = Enum.GetNames<T>();

            for (var i = 0; i < enumValues.Length; ++i)
            {
                var value = enumValues[i];
                var name = enumNames[i];

                ref var storedName = ref CollectionsMarshal.GetValueRefOrAddDefault(names, value, out var nameAlreadyStored);
                if (!nameAlreadyStored || storedName is null || name.Length < storedName.Length)
                {
                    storedName = name;
                }
            }

            return names.ToFrozenDictionary();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the enum type supports flags.
    /// </summary>
    public static bool HasFlags { get; }

    /// <summary>
    /// Gets the maximum length of the enum value string representation.
    /// </summary>
    public static int MaxLength { get; }

    /// <summary>
    /// Gets or sets the enum value separator character.
    /// </summary>
    public static char ValueSeparator { get; set; } = DefaultValueSeparator;

    /// <summary>
    /// Converts a value to a string representation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The string representation.</returns>
    public static string ToString(T value)
    {
        if (HasFlags && !value.Equals(default(T)))
        {
            return String.Join(ValueSeparator, names.Keys.Where(k => value.HasFlag(k) && !k.Equals(default(T))).Select(k => names[k]));
        }

        return names[value];
    }

    /// <summary>
    /// Converts the <typeparamref name="T"/> value to a <see cref="DbString"/> value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="DbString"/> value that represents the <typeparamref name="T"/> value.</returns>
    public static DbString ToDbString(T value)
    {
        return ToString(value).AsVarChar(MaxLength);
    }

    /// <summary>
    /// Tries to format the value as a string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
    public static bool TryFormat(T value, Span<char> destination, out int charsWritten)
    {
        if (HasFlags && !value.Equals(default(T)))
        {
            charsWritten = 0;

            foreach (var nv in names)
            {
                if (value.HasFlag(nv.Key) && !nv.Key.Equals(default(T)))
                {
                    if (charsWritten > 0)
                    {
                        if (charsWritten == destination.Length)
                            return false;

                        destination[charsWritten++] = ValueSeparator;
                    }
                    if (nv.Value.TryCopyTo(destination[charsWritten..]))
                    {
                        charsWritten += nv.Value.Length;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        var valueName = names[value];
        if (valueName.TryCopyTo(destination))
        {
            charsWritten = valueName.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Converts the string representation of the name of one or more enumerated constants
    /// to an equivalent enumerated object. A parameter specifies whether the operation is case-sensitive.
    /// The return value indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="value">The span representation of the name of one or more enumerated constants.</param>
    /// <param name="ignoreCase"><c>true</c> to ignore case; <c>false</c> to consider case.</param>
    /// <param name="result">When this method returns true, contains an enumeration constant that represents the parsed value.</param>
    /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, bool ignoreCase, out T result)
    {
        if (value.IsEmpty)
        {
            result = default;
            return false;
        }

        if (HasFlags && ValueSeparator != DefaultValueSeparator && value.Contains(ValueSeparator))
        {
            Span<char> normalizedValue = stackalloc char[value.Length];
            value.Replace(normalizedValue, ValueSeparator, DefaultValueSeparator);

            return Enum.TryParse(normalizedValue, ignoreCase, out result);
        }

        return Enum.TryParse(value, ignoreCase, out result);
    }
}
