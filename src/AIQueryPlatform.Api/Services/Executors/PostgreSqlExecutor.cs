using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Npgsql;
using System.Data;

namespace AIQueryPlatform.Api.Services.Executors;

/// <summary>
/// PostgreSQL specific query executor
/// Requires Npgsql NuGet package
/// </summary>
public class PostgreSqlExecutor : IDatabaseExecutor
{
    private readonly ILogger<PostgreSqlExecutor> _logger;

    public DatabaseType SupportedDatabaseType => DatabaseType.PostgreSql;

    public PostgreSqlExecutor(ILogger<PostgreSqlExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, int timeout = 30)
    {
        var result = new QueryResult();

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand(sql, connection)
            {
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            };

            _logger.LogInformation("Executing PostgreSQL query");

            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            // Read all rows
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }
                
                result.Rows.Add(row);
            }

            result.RowCount = result.Rows.Count;

            _logger.LogInformation("PostgreSQL query executed successfully. Rows returned: {RowCount}", result.RowCount);

            return result;
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "PostgreSQL execution error: {Message}", ex.Message);
            throw new InvalidOperationException($"PostgreSQL error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error");
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }
}
