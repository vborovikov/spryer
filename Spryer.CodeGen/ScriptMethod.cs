namespace Spryer.CodeGen;

using System;
using System.Data;
using System.Linq;
using Dapper;

sealed record ScriptMethod(DbScript Script) : ICodeGenerator
{
    private const string Cnn = "connection";
    private const string Tx = "transaction";

    public void Generate(CodeBuilder code)
    {
        var methodName = this.Script.Name.ToPascalCase();

        // method signature
        code.Append($"public static {GetDapperMethodReturnType()} {methodName}Async{GetDapperMethodGenericType()}(this IDbConnection {Cnn}");
        var parameters = GetParameters();
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            code.Append(',')
                .AppendLine()
                .IncrementIndent()
                .Append(parameters)
                .DecrementIndent();
        }
        code.Append(')').AppendLine();

        // method body
        code.Append('{').AppendLine();
        var arguments = GetArguments();
        using (code.Indent())
        {
            code.Append($"return {Cnn}.{GetDapperMethod()}(sql[\"{this.Script.Name}\"]");
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                code.Append(',')
                    .AppendLine()
                    .IncrementIndent()
                    .AppendLine("param: new")
                    .Append('{').AppendLine()
                    .IncrementIndent()
                    .AppendLines(arguments)
                    .DecrementIndent()
                    .Append('}')
                    .DecrementIndent();
            }
            code.AppendLine(");");
        }
        code.Append('}').AppendLine();

        if (this.Script.Type is DbScriptType.Execute or DbScriptType.ExecuteReader or DbScriptType.ExecuteScalar)
        {
            // method signature
            code.AppendLine()
                .Append($"public static {GetDapperMethodReturnType()} {methodName}Async{GetDapperMethodGenericType()}(this IDbTransaction {Tx}");
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                code.Append(',')
                    .AppendLine()
                    .IncrementIndent()
                    .Append(parameters)
                    .DecrementIndent();
            }
            code.Append(')').AppendLine();

            // method body
            code.Append('{').AppendLine();
            using (code.Indent())
            {
                code.Append($"return {Tx}.Connection!.{GetDapperMethod()}(sql[\"{this.Script.Name}\"]");
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    code.Append(',')
                        .AppendLine()
                        .IncrementIndent()
                        .AppendLine("param: new")
                        .Append('{').AppendLine()
                        .IncrementIndent()
                        .AppendLines(arguments)
                        .DecrementIndent()
                        .Append('}')
                        .DecrementIndent();
                }
                code.AppendLine($", transaction: {Tx});");
            }
            code.Append('}').AppendLine();
        }
    }

    private string GetArguments()
    {
        return string.Join($",{Environment.NewLine}",
            this.Script.Parameters.Select(p =>
                $"{p.Name.ToPascalCase()} = {GetParamValue(p)}"));
    }

    private string GetParameters()
    {
        return string.Join(", ", this.Script.Parameters.Select(p => $"{GetParamType(p)} {p.Name.ToCamelCase()}"));
    }

    private static string GetParamValue(DbScriptParameter p)
    {
        var paramName = p.Name.ToCamelCase();

        paramName += p.Type switch
        {
            DbType.AnsiString => p.Size > 0 ? $".AsVarChar({p.Size})" : ".AsVarChar()",
            DbType.AnsiStringFixedLength => p.Size > 0 ? $".AsChar({p.Size})" : ".AsChar()",
            DbType.StringFixedLength => p.Size > 0 ? $".AsNChar({p.Size})" : ".AsNChar()",
            DbType.String or DbType.VarNumeric or DbType.Xml => p.Size > 0 ? $".AsNVarChar({p.Size})" : ".AsNVarChar()",
            _ => string.Empty
        };

        return paramName;
    }

    private static string GetParamType(DbScriptParameter p)
    {
        return p.Type switch
        {
            DbType.Boolean => "bool",
            DbType.Byte => "byte",
            DbType.Int16 => "short",
            DbType.Single => "float",
            DbType.UInt16 => "ushort",
            DbType.UInt32 => "uint",
            DbType.UInt64 => "ulong",
            DbType.SByte => "sbyte",
            DbType.Int32 => "int",
            DbType.Int64 => "long",
            DbType.Double => "double",
            DbType.Decimal or
            DbType.Currency => "decimal",
            DbType.String or
            DbType.AnsiString or
            DbType.StringFixedLength or
            DbType.AnsiStringFixedLength or
            DbType.VarNumeric or
            DbType.Xml => "string",
            DbType.DateTime or
            DbType.DateTime2 or
            DbType.Date or
            DbType.Time => "DateTime",
            DbType.DateTimeOffset => "DateTimeOffset",
            DbType.Binary => "byte[]",
            DbType.Guid => "Guid",
            _ => string.IsNullOrWhiteSpace(p.CustomType) ? "object" : p.CustomType
        };
    }

    private string GetDapperMethod()
    {
        return this.Script.Type switch
        {
            DbScriptType.Execute => nameof(SqlMapper.ExecuteAsync),
            DbScriptType.ExecuteReader => nameof(SqlMapper.ExecuteReaderAsync),
            DbScriptType.ExecuteScalar => nameof(SqlMapper.ExecuteScalarAsync),
            DbScriptType.Query => nameof(SqlMapper.QueryAsync),
            DbScriptType.QueryFirst => nameof(SqlMapper.QueryFirstAsync),
            DbScriptType.QueryFirstOrDefault => nameof(SqlMapper.QueryFirstOrDefaultAsync),
            DbScriptType.QuerySingle => nameof(SqlMapper.QuerySingleAsync),
            DbScriptType.QuerySingleOrDefault => nameof(SqlMapper.QuerySingleOrDefaultAsync),
            DbScriptType.QueryMultiple => nameof(SqlMapper.QueryMultipleAsync),
            DbScriptType.QueryUnbuffered => nameof(SqlMapper.QueryUnbufferedAsync),
            DbScriptType.QueryText => "QueryTextAsync",
            DbScriptType.QueryJson => "QueryJsonAsync",
            _ => nameof(SqlMapper.ExecuteAsync)
        } + GetDapperMethodGenericType();
    }

    private string GetDapperMethodReturnType()
    {
        return this.Script.Type switch
        {
            DbScriptType.Execute => "Task<int>",
            DbScriptType.ExecuteReader => "Task<IDataReader>",
            DbScriptType.ExecuteScalar => "Task<T?>",
            DbScriptType.Query => "Task<IEnumerable<T>>",
            DbScriptType.QueryFirst => "Task<T>",
            DbScriptType.QueryFirstOrDefault => "Task<T?>",
            DbScriptType.QuerySingle => "Task<T>",
            DbScriptType.QuerySingleOrDefault => "Task<T?>",
            DbScriptType.QueryMultiple => "Task<SqlMapper.GridReader>",
            DbScriptType.QueryUnbuffered => "IAsyncEnumerable<T>",
            DbScriptType.QueryText => "Task<string>",
            DbScriptType.QueryJson => "Task<T?>",
            _ => "Task<int>"
        };
    }

    private string GetDapperMethodGenericType()
    {
        return this.Script.Type switch
        {
            DbScriptType.Execute => string.Empty,
            DbScriptType.ExecuteReader => string.Empty,
            DbScriptType.ExecuteScalar => "<T>",
            DbScriptType.Query => "<T>",
            DbScriptType.QueryFirst => "<T>",
            DbScriptType.QueryFirstOrDefault => "<T>",
            DbScriptType.QuerySingle => "<T>",
            DbScriptType.QuerySingleOrDefault => "<T>",
            DbScriptType.QueryMultiple => string.Empty,
            DbScriptType.QueryUnbuffered => "<T>",
            DbScriptType.QueryText => string.Empty,
            DbScriptType.QueryJson => "<T>",
            _ => string.Empty
        };
    }
}
