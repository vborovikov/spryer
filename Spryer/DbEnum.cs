namespace Spryer;

using System;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

[DbEnumJsonConverter]
public readonly struct DbEnum<TEnum> : IEquatable<TEnum>, IEquatable<DbEnum<TEnum>>
    where TEnum : struct, Enum
{
    private sealed class JsonConverter : JsonConverter<DbEnum<TEnum>>
    {
        public override DbEnum<TEnum> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DbEnum<TEnum> value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

    private sealed class DbEnumTypeHandler : SqlMapper.TypeHandler<DbEnum<TEnum>>
    {
        public override DbEnum<TEnum> Parse(object value) => new(value as string);
        public override void SetValue(IDbDataParameter parameter, DbEnum<TEnum> value)
        {
            parameter.DbType = DbType.AnsiStringFixedLength;
            parameter.Size = DbEnum<TEnum>.MaxLength;
            parameter.Value = value.ToString();
        }
    }

    private sealed class DbNullableEnumTypeHandler : SqlMapper.TypeHandler<DbEnum<TEnum>?>
    {
        public override DbEnum<TEnum>? Parse(object value) => value switch
        {
            string str => new DbEnum<TEnum>(str),
            _ => null!
        };

        public override void SetValue(IDbDataParameter parameter, DbEnum<TEnum>? value)
        {
            parameter.DbType = DbType.AnsiStringFixedLength;
            parameter.Size = DbEnum<TEnum>.MaxLength;
            parameter.Value = value is not null ? value.ToString() : DBNull.Value;
        }
    }

    private readonly TEnum value;

    public DbEnum(TEnum value)
    {
        this.value = value;
    }

    public DbEnum(string? name)
        : this(name.AsSpan()) { }

    public DbEnum(ReadOnlySpan<char> name)
    {
        this.value = Enum.TryParse<TEnum>(name, ignoreCase: true, out var value) ? value : default;
    }

    public static void Initialize()
    {
        SqlMapper.AddTypeHandler(new DbEnumTypeHandler());
        SqlMapper.AddTypeHandler(new DbNullableEnumTypeHandler());
    }

    public static implicit operator DbEnum<TEnum>(TEnum value) => new(value);

    public static implicit operator TEnum(DbEnum<TEnum> value) => value.value;

    public static implicit operator DbEnum<TEnum>(string name) => new(name);

    public static int MaxLength => EnumInfo<TEnum>.MaxLength;

    public override string ToString()
    {
        return EnumInfo<TEnum>.ToString(this.value);
    }

    public override int GetHashCode()
    {
        return this.value.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return
            (obj is TEnum otherEnum && Equals(otherEnum)) ||
            (obj is DbEnum<TEnum> other && Equals(other));
    }

    public bool Equals(DbEnum<TEnum> other)
    {
        return this.value.Equals(other.value);
    }

    public bool Equals(TEnum other)
    {
        return this.value.Equals(other);
    }

    public bool HasFlag(TEnum flag) => this.value.HasFlag(flag);

    private static JsonConverter CreateJsonConverter() => new();
}

[AttributeUsage(AttributeTargets.Struct)]
internal sealed class DbEnumJsonConverterAttribute : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        var createConverterMethod = typeToConvert.GetMethod("CreateJsonConverter", BindingFlags.NonPublic | BindingFlags.Static);
        ArgumentNullException.ThrowIfNull(createConverterMethod);
        return (JsonConverter)createConverterMethod.Invoke(null, null)!;
    }
}
