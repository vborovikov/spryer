namespace Spryer;

using System;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

/// <summary>
/// Represents a database enum value.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
[DbEnumJsonConverter]
public readonly struct DbEnum<TEnum> : IEquatable<TEnum>, IEquatable<DbEnum<TEnum>>
    where TEnum : struct, Enum
{
    /// <summary>
    /// A JSON converter for <see cref="DbEnum{TEnum}"/> instances.
    /// </summary>
    private sealed class JsonConverter : JsonConverter<DbEnum<TEnum>>
    {
        public override DbEnum<TEnum> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            Span<char> buffer = stackalloc char[EnumInfo<TEnum>.MaxLength];
            var charsWritten = reader.CopyString(buffer);
            ReadOnlySpan<char> source = buffer[..charsWritten];
            return new(source);
        }

        public override void Write(Utf8JsonWriter writer, DbEnum<TEnum> value, JsonSerializerOptions options)
        {
            Span<char> buffer = stackalloc char[EnumInfo<TEnum>.MaxLength];
            EnumInfo<TEnum>.TryFormat(value, buffer, out var charsWritten);
            ReadOnlySpan<char> source = buffer[..charsWritten];
            writer.WriteStringValue(source);
        }
    }

    /// <summary>
    /// A type handler for <see cref="DbEnum{TEnum}"/> instances to be used with Dapper.
    /// </summary>
    private sealed class DbEnumTypeHandler : SqlMapper.TypeHandler<DbEnum<TEnum>>
    {
        public override DbEnum<TEnum> Parse(object value) => new(value as string);
        public override void SetValue(IDbDataParameter parameter, DbEnum<TEnum> value)
        {
            parameter.DbType = EnumInfo<TEnum>.HasFlags ? DbType.AnsiString : DbType.AnsiStringFixedLength;
            parameter.Size = EnumInfo<TEnum>.MaxLength;
            parameter.Value = value.ToString();
        }
    }

    /// <summary>
    /// A type handler for nullable <see cref="DbEnum{TEnum}"/> instances to be used with Dapper.
    /// </summary>
    private sealed class DbNullableEnumTypeHandler : SqlMapper.TypeHandler<DbEnum<TEnum>?>
    {
        public override DbEnum<TEnum>? Parse(object value) => value switch
        {
            string str => new DbEnum<TEnum>(str),
            _ => null!
        };

        public override void SetValue(IDbDataParameter parameter, DbEnum<TEnum>? value)
        {
            parameter.DbType = EnumInfo<TEnum>.HasFlags ? DbType.AnsiString : DbType.AnsiStringFixedLength;
            parameter.Size = EnumInfo<TEnum>.MaxLength;
            parameter.Value = value is not null ? value.Value.ToString() : DBNull.Value;
        }
    }

    /// <summary>
    /// The database enum value.
    /// </summary>
    private readonly TEnum value;

    /// <summary>
    /// Creates a new <see cref="DbEnum{TEnum}"/> instance with the specified value.
    /// </summary>
    public DbEnum(TEnum value)
    {
        this.value = value;
    }

    /// <summary>
    /// Creates a new <see cref="DbEnum{TEnum}"/> instance with the specified name.
    /// </summary>
    public DbEnum(string? name)
        : this(name.AsSpan()) { }

    /// <summary>
    /// Creates a new <see cref="DbEnum{TEnum}"/> instance with the specified name.
    /// </summary>
    public DbEnum(ReadOnlySpan<char> name)
    {
        this.value = EnumInfo<TEnum>.TryParse(name, ignoreCase: true, out var value) ? value : default;
    }

    /// <summary>
    /// Initializes the Dapper type handlers.
    /// </summary>
    /// <param name="valueSeparator">The enum value separator character.</param>
    public static void Initialize(char valueSeparator = default)
    {
        EnumInfo<TEnum>.ValueSeparator = valueSeparator != default ? valueSeparator : EnumInfo<TEnum>.DefaultValueSeparator;

        SqlMapper.AddTypeHandler(new DbEnumTypeHandler());
        SqlMapper.AddTypeHandler(new DbNullableEnumTypeHandler());
    }

    /// <summary>
    /// Implicitly converts a <typeparamref name="TEnum"/> value to a <see cref="DbEnum{TEnum}"/> instance.
    /// </summary>
    public static implicit operator DbEnum<TEnum>(TEnum value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="DbEnum{TEnum}"/> instance to a <typeparamref name="TEnum"/> value.
    /// </summary>
    public static implicit operator TEnum(DbEnum<TEnum> value) => value.value;

    /// <summary>
    /// Implicitly converts a name to a <see cref="DbEnum{TEnum}"/> instance.
    /// </summary>
    public static implicit operator DbEnum<TEnum>(string name) => new(name);

    /// <summary>
    /// Returns a string that represents the current <see cref="DbEnum{TEnum}"/> value.
    /// </summary>
    /// <returns>The string representation of the current <see cref="DbEnum{TEnum}"/> value.</returns>
    public override string ToString()
    {
        return EnumInfo<TEnum>.ToString(this.value);
    }

    /// <summary>
    /// Converts the <see cref="DbEnum{TEnum}"/> value to a <see cref="DbString"/> value.
    /// </summary>
    /// <returns>The <see cref="DbString"/> value that represents the current <see cref="DbEnum{TEnum}"/> value.</returns>
    public DbString ToDbString()
    {
        return EnumInfo<TEnum>.ToDbString(this.value);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.value.GetHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return
            (obj is TEnum otherEnum && Equals(otherEnum)) ||
            (obj is DbEnum<TEnum> other && Equals(other));
    }

    /// <inheritdoc/>
    public bool Equals(DbEnum<TEnum> other)
    {
        return this.value.Equals(other.value);
    }

    /// <inheritdoc/>
    public bool Equals(TEnum other)
    {
        return this.value.Equals(other);
    }

    /// <summary>
    /// Determines whether the current <see cref="DbEnum{TEnum}"/> instance has the specified flag.
    /// </summary>
    public bool HasFlag(TEnum flag) => this.value.HasFlag(flag);

    /// <summary>
    /// Creates a new <see cref="JsonConverter"/> instance for the current <see cref="DbEnum{TEnum}"/> type.
    /// </summary>
    private static JsonConverter CreateJsonConverter() => new();
}

/// <summary>
/// An attribute used to decorate a struct with a <see cref="DbEnum{TEnum}"/> JSON converter.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
internal sealed class DbEnumJsonConverterAttribute : JsonConverterAttribute
{
    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        var createConverterMethod = typeToConvert.GetMethod("CreateJsonConverter", BindingFlags.NonPublic | BindingFlags.Static);
        ArgumentNullException.ThrowIfNull(createConverterMethod);
        return (JsonConverter)createConverterMethod.Invoke(null, null)!;
    }
}
