namespace Spryer;

using Dapper;

/// <summary>
/// A static class containing extension methods for Dapper.
/// </summary>
public static class DapperExtensions
{   
    /// <summary>
    /// Converts a string value to a <see cref="DbString"/> instance with a fixed length, using the ANSI encoding.
    /// </summary>
    /// <param name="str">The string value to convert.</param>
    /// <param name="maxLength">The maximum length of the resulting <see cref="DbString"/> instance.</param>
    /// <returns>A <see cref="DbString"/> instance with a fixed length, using the ANSI encoding.</returns>
    public static DbString AsChar(this string? str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: true, isAnsi: true);

    /// <summary>
    /// Converts a string value to a <see cref="DbString"/> instance with a variable length, using the ANSI encoding.
    /// </summary>
    /// <param name="str">The string value to convert.</param>
    /// <param name="maxLength">The maximum length of the resulting <see cref="DbString"/> instance.</param>
    /// <returns>A <see cref="DbString"/> instance with a variable length, using the ANSI encoding.</returns>
    public static DbString AsVarChar(this string? str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: false, isAnsi: true);

    /// <summary>
    /// Converts a string value to a <see cref="DbString"/> instance with a fixed length, using the Unicode encoding.
    /// </summary>
    /// <param name="str">The string value to convert.</param>
    /// <param name="maxLength">The maximum length of the resulting <see cref="DbString"/> instance.</param>
    /// <returns>A <see cref="DbString"/> instance with a fixed length, using the Unicode encoding.</returns>
    public static DbString AsNChar(this string? str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: true, isAnsi: false);

    /// <summary>
    /// Converts a string value to a <see cref="DbString"/> instance with a variable length, using the Unicode encoding.
    /// </summary>
    /// <param name="str">The string value to convert.</param>
    /// <param name="maxLength">The maximum length of the resulting <see cref="DbString"/> instance.</param>
    /// <returns>A <see cref="DbString"/> instance with a variable length, using the Unicode encoding.</returns>
    public static DbString AsNVarChar(this string? str, int maxLength = -1) => str.AsDbString(maxLength, isFixedLength: false, isAnsi: false);

    /// <summary>
    /// Converts a string value to a <see cref="DbString"/> instance with the specified properties.
    /// </summary>
    /// <param name="str">The string value to convert.</param>
    /// <param name="maxLength">The maximum length of the resulting <see cref="DbString"/> instance.</param>
    /// <param name="isFixedLength">Whether the resulting <see cref="DbString"/> instance is fixed length.</param>
    /// <param name="isAnsi">Whether the resulting <see cref="DbString"/> instance uses ANSI encoding.</param>
    /// <returns>A <see cref="DbString"/> instance with the specified properties.</returns>
    public static DbString AsDbString(this string? str, int maxLength = -1, bool isFixedLength = false, bool isAnsi = false) => new DbString
    {
        Value = maxLength > 0 && str?.Length > maxLength ? str[..maxLength] : str,
        IsFixedLength = maxLength > 0 && isFixedLength,
        Length = maxLength < 0 ? -1 : maxLength,
        IsAnsi = isAnsi,
    };

    /// <summary>
    /// Converts an enumeration value to a <see cref="DbEnum{TEnum}"/> instance.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="en">The enumeration value to convert.</param>
    /// <returns>A <see cref="DbEnum{TEnum}"/> instance.</returns>
    public static DbEnum<TEnum> AsDbEnum<TEnum>(this TEnum en) where TEnum : struct, Enum => new(en);
}