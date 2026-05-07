using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AIQueryPlatform.Api.Services.Executors;

/// <summary>
/// SQL Server specific query executor
/// </summary>
public class SqlServerExecutor : IDatabaseExecutor
{
    private readonly ILogger<SqlServerExecutor> _logger;

    public DatabaseType SupportedDatabaseType => DatabaseType.SqlServer;

    public SqlServerExecutor(ILogger<SqlServerExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, int timeout = 30)
    {
        var result = new QueryResult();

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            };

            _logger.LogInformation("Executing SQL Server query");

            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable != null)
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    result.Columns.Add(row["ColumnName"].ToString() ?? "Unknown");
                }
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

            _logger.LogInformation("SQL Server query executed successfully. Rows returned: {RowCount}", result.RowCount);

            return result;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server execution error: {Message}", ex.Message);
            throw new InvalidOperationException($"SQL Server error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error");
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }
}
