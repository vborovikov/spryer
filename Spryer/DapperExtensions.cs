namespace Spryer;

using Dapper;

public static class DapperExtensions
{   
    public static DbString AsChar(this string str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: true, isAnsi: true);

    public static DbString AsVarChar(this string str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: false, isAnsi: true);

    public static DbString AsNChar(this string str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: true, isAnsi: false);

    public static DbString AsNVarChar(this string str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: false, isAnsi: false);

    public static DbString AsDbString(this string str, int maxLength = -1, bool isFixedLength = false, bool isAnsi = false) => new DbString
    {
        Value = maxLength > 0 && str?.Length > maxLength ? str[..maxLength] : str,
        IsFixedLength = maxLength > 0 && isFixedLength,
        Length = maxLength < 0 ? -1 : maxLength,
        IsAnsi = isAnsi,
    };

    public static DbEnum<TEnum> AsDbEnum<TEnum>(this TEnum en) where TEnum : struct, Enum => new(en);
}