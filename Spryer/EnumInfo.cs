namespace Spryer;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides the enumeration type information.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
internal static class EnumInfo<T>
    where T : struct, Enum
{
    private const char ValueSeparator = ',';
    private static readonly Dictionary<T, string> names;

    /// <summary>
    /// Initializes static members of the <see cref="EnumInfo{T}"/> class.
    /// </summary>
    static EnumInfo()
    {
        HasFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
        names = new Dictionary<T, string>();

        var enumValues = Enum.GetValues<T>();
        var enumNames = Enum.GetNames<T>();

        for (var i = 0; i < enumValues.Length; ++i)
        {
            var value = enumValues[i];
            var name = enumNames[i];

            if (names.ContainsKey(value))
            {
                if (name.Length < names[value].Length)
                {
                    names[value] = name;
                }
            }
            else
            {
                names.Add(enumValues[i], enumNames[i]);
            }
        }

        MaxLength = HasFlags ?
            names.Values.Sum(x => x.Length) + names.Count - 1 :
            names.Values.Max(x => x.Length);
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
    /// Converts a value to a string representation.
    /// </summary>
    public static string ToString(T value)
    {
        if (HasFlags && !value.Equals(default(T)))
        {
            return String.Join(ValueSeparator, names.Keys.Where(k => value.HasFlag(k) && !k.Equals(default(T))).Select(k => names[k]));
        }

        return names[value];
    }
}
