namespace Spryer;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class EnumInfo<T>
    where T : struct, Enum
{
    private const char ValueSeparator = ',';
    private static readonly Dictionary<T, string> names;

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

    public static bool HasFlags { get; }

    public static int MaxLength { get; }

    public static string ToString(T value)
    {
        if (HasFlags && !value.Equals(default(T)))
        {
            return String.Join(ValueSeparator, names.Keys.Where(k => value.HasFlag(k) && !k.Equals(default(T))).Select(k => names[k]));
        }

        return names[value];
    }
}
