namespace Spryer;

using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;

/// <summary>
/// Provides <see cref="SqlMapper"/> extension methods.
/// </summary>
public static class SqlMapperExtensions
{
    private const int DefaultTextCapacity = 1024 * 3;

    /// <summary>
    /// Executes a query and returns the result as a single string.
    /// </summary>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="transaction">The transaction to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <returns>The result of the query as a single string.</returns>
    public static async Task<string> QueryTextAsync(this DbConnection cnn, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        await using var reader = await cnn.ExecuteReaderAsync(sql, param, transaction, commandTimeout, commandType);
        var text = new StringBuilder(DefaultTextCapacity);
        while (await reader.ReadAsync())
        {
            text.Append(reader.GetString(0));
        }
        return text.ToString();
    }

    /// <summary>
    /// Executes a query and returns the result as a deserialized JSON object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="transaction">The transaction to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="jsonOptions">The JSON serializer options to use.</param>
    /// <returns>The result of the query as a deserialized JSON object.</returns>
    public static async Task<T?> QueryJsonAsync<T>(this DbConnection cnn, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null, JsonSerializerOptions? jsonOptions = null)
    {
        //todo: use Utf8JsonReader/GrowBuffer

        var json = await cnn.QueryTextAsync(sql, param, transaction, commandTimeout, commandType);
        if (json.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(json, jsonOptions);
    }

    /// <summary>
    /// Executes a query and returns the result as a single string.
    /// </summary>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="transaction">The transaction to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <returns>The result of the query as a single string.</returns>
    public static async Task<string> QueryTextAsync(this IDbConnection cnn, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        using var reader = await cnn.ExecuteReaderAsync(sql, param, transaction, commandTimeout, commandType);
        var text = new StringBuilder(DefaultTextCapacity);
        while (reader.Read())
        {
            text.Append(reader.GetString(0));
        }
        return text.ToString();
    }

    /// <summary>
    /// Executes a query and returns the result as a deserialized JSON object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="transaction">The transaction to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="jsonOptions">The JSON serializer options to use.</param>
    /// <returns>The result of the query as a deserialized JSON object.</returns>
    public static async Task<T?> QueryJsonAsync<T>(this IDbConnection cnn, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null, JsonSerializerOptions? jsonOptions = null)
    {
        var json = await cnn.QueryTextAsync(sql, param, transaction, commandTimeout, commandType);
        if (json.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(json, jsonOptions);
    }
}
