namespace Spryer;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class EnumInfo<T>
    where T : struct, Enum
{
    private const char ValueSeparator = ',';

    private static readonly bool isFlags;
    private static readonly Dictionary<T, string> names;
    private static readonly int maxNameLength;

    static EnumInfo()
    {
        isFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
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

        maxNameLength = names.Values.Max(x => x.Length);
    }

    public static int MaxLength => maxNameLength * (isFlags ? names.Count : 1) + (isFlags ? names.Count - 1 : 0);

    public static string ToString(T value)
    {
        if (isFlags && !value.Equals(default(T)))
        {
            return String.Join(ValueSeparator, names.Keys.Where(k => value.HasFlag(k) && !k.Equals(default(T))).Select(k => names[k]));
        }

        return names[value];
    }
}
