using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using MySql.Data.MySqlClient;
using System.Data;

namespace AIQueryPlatform.Api.Services.Executors;

/// <summary>
/// MySQL specific query executor
/// Requires MySql.Data NuGet package
/// </summary>
public class MySqlExecutor : IDatabaseExecutor
{
    private readonly ILogger<MySqlExecutor> _logger;

    public DatabaseType SupportedDatabaseType => DatabaseType.MySql;

    public MySqlExecutor(ILogger<MySqlExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, int timeout = 30)
    {
        var result = new QueryResult();

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection)
            {
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            };

            _logger.LogInformation("Executing MySQL query");

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

            _logger.LogInformation("MySQL query executed successfully. Rows returned: {RowCount}", result.RowCount);

            return result;
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "MySQL execution error: {Message}", ex.Message);
            throw new InvalidOperationException($"MySQL error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error");
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }
}
