namespace Spryer;

using Dapper;

public static class DapperExtensions
{
    public static DbString AsDbString(this string str, int maxLength = -1, bool isAnsi = false)
    {
        return new DbString
        {
            Value = maxLength > 0 && str?.Length > maxLength ? str[..maxLength] : str,
            IsFixedLength = maxLength > 0,
            Length = maxLength < 0 ? -1 : maxLength,
            IsAnsi = isAnsi,
        };
    }
}