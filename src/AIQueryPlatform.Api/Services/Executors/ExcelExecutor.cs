using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Data;
using System.Data.OleDb;

namespace AIQueryPlatform.Api.Services.Executors;

/// <summary>
/// Excel specific query executor using OleDb
/// Requires System.Data.OleDb (Windows only)
/// </summary>
public class ExcelExecutor : IDatabaseExecutor
{
    private readonly ILogger<ExcelExecutor> _logger;

    public DatabaseType SupportedDatabaseType => DatabaseType.Excel;

    public ExcelExecutor(ILogger<ExcelExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, int timeout = 30)
    {
        var result = new QueryResult();

        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync();

            using var command = new OleDbCommand(sql, connection)
            {
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            };

            _logger.LogInformation("Executing Excel OleDb query");

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

            _logger.LogInformation("Excel query executed successfully. Rows returned: {RowCount}", result.RowCount);

            return result;
        }
        catch (OleDbException ex)
        {
            _logger.LogError(ex, "Excel OleDb execution error: {Message}", ex.Message);
            throw new InvalidOperationException($"Excel error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error");
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }
}
