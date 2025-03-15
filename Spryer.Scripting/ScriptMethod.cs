namespace Spryer.Scripting;

using System;
using System.Data;
using System.Linq;

sealed record ScriptMethod(DbScript Script) : ICodeGenerator
{
    private const string Cnn = "connection";
    private const string Tx = "transaction";

    public bool IsInline { get; set; }

    public void Generate(CodeBuilder code)
    {
        var methodName = GetMethodName();

        //todo: generate xml comments

        // method signature
        code.Append($"public static {GetDapperMethodReturnType()} {methodName}{GetDapperMethodGenericType()}(this DbConnection {Cnn}");
        var parameters = GetParameters();
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            code.Append(',')
                .AppendLine()
                .IncrementIndent()
                .Append(parameters)
                .DecrementIndent();
        }

        // SqlMapper parameters
        code.Append(',')
            .AppendLine()
            .IncrementIndent()
            .Append("IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)")
            .AppendLine()
            .DecrementIndent();

        // method body
        code.Append('{').AppendLine();
        var arguments = GetArguments();
        using (code.Indent())
        {
            code.Append($"return {Cnn}.{GetDapperMethod()}(");
            if (this.IsInline)
            {
                code.AppendLine()
                    .IncrementIndent()
                    .AppendLine("\"\"\"")
                    .AppendLines(this.Script.Text)
                    .Append("\"\"\"")
                    .DecrementIndent();
            }
            else
            {
                code.Append($"sql[\"{this.Script.Name}\"]");
            }

            if (this.Script.Parameters is [{ Name: "Parameters", Type: DbType.Object } dynamicParams])
            {
                // pass (@Parameters object) as is
                code.Append(',')
                    .AppendLine()
                    .IncrementIndent()
                    .Append($"param: {dynamicParams.Name.ToCamelCase()}")
                    .DecrementIndent();

            }
            else if (!string.IsNullOrWhiteSpace(arguments))
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

            // SqlMapper arguments

            code.AppendLine(",")
                .IncrementIndent()
                .AppendLine("transaction: transaction, commandTimeout: commandTimeout, commandType: commandType);")
                .DecrementIndent();
        }
        code.Append('}').AppendLine();

        if (this.Script.Type is DbScriptType.Execute or DbScriptType.ExecuteReader or DbScriptType.ExecuteScalar)
        {
            // method signature
            code.AppendLine()
                .Append($"public static {GetDapperMethodReturnType()} {methodName}{GetDapperMethodGenericType()}(this DbTransaction {Tx}");
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                code.Append(',')
                    .AppendLine()
                    .IncrementIndent()
                    .Append(parameters)
                    .DecrementIndent();
            }
            // SqlMapper parameters
            code.Append(',')
                .AppendLine()
                .IncrementIndent()
                .Append("int? commandTimeout = null, CommandType? commandType = null)")
                .AppendLine()
                .DecrementIndent();

            // method body
            code.Append('{').AppendLine();
            using (code.Indent())
            {
                code.Append($"return {methodName}{GetDapperMethodGenericType()}({Tx}.Connection!");
                if (this.Script.Parameters.Length > 0)
                {
                    code.Append(',')
                        .AppendLine()
                        .IncrementIndent()
                        .Append(string.Join(", ", this.Script.Parameters.Select(p => p.Name.ToCamelCase())))
                        .DecrementIndent();
                }

                // SqlMapper arguments
                code.AppendLine(",")
                    .IncrementIndent()
                    .AppendLine($"transaction: {Tx}, commandTimeout: commandTimeout, commandType: commandType);")
                    .DecrementIndent();

            }
            code.Append('}').AppendLine();
        }
    }

    private string GetMethodName()
    {
        return $"{this.Script.Name.ToPascalCase()}Async";
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
            DbType.Xml => "string?",
            DbType.DateTime or
            DbType.DateTime2 or
            DbType.Date or
            DbType.Time => "DateTime",
            DbType.DateTimeOffset => "DateTimeOffset",
            DbType.Binary => "byte[]",
            DbType.Guid => "Guid?",
            _ => string.IsNullOrWhiteSpace(p.CustomType) ? "object" : p.CustomType ?? "object?"
        };
    }

    private string GetDapperMethod()
    {
        return this.Script.Type switch
        {
            DbScriptType.Execute => "ExecuteAsync",
            DbScriptType.ExecuteReader => "ExecuteReaderAsync",
            DbScriptType.ExecuteScalar => "ExecuteScalarAsync",
            DbScriptType.Query => "QueryAsync",
            DbScriptType.QueryFirst => "QueryFirstAsync",
            DbScriptType.QueryFirstOrDefault => "QueryFirstOrDefaultAsync",
            DbScriptType.QuerySingle => "QuerySingleAsync",
            DbScriptType.QuerySingleOrDefault => "QuerySingleOrDefaultAsync",
            DbScriptType.QueryMultiple => "QueryMultipleAsync",
            DbScriptType.QueryUnbuffered => "QueryUnbufferedAsync",
            DbScriptType.QueryText => "QueryTextAsync",
            DbScriptType.QueryJson => "QueryJsonAsync",
            _ => "ExecuteAsync"
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
