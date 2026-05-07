using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for executing SQL queries against tenant databases
/// </summary>
public class QueryExecutionService : IQueryExecutionService
{
    private readonly ILogger<QueryExecutionService> _logger;
    private readonly int _queryTimeoutSeconds;

    public QueryExecutionService(
        IConfiguration configuration,
        ILogger<QueryExecutionService> logger)
    {
        _logger = logger;
        _queryTimeoutSeconds = configuration.GetValue<int>("QueryExecution:QueryTimeoutSeconds", 30);
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString)
    {
        var result = new QueryResult();

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = _queryTimeoutSeconds,
                CommandType = CommandType.Text
            };

            _logger.LogInformation("Executing SQL query");

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

            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", result.RowCount);

            return result;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL execution error: {Message}", ex.Message);
            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error");
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }
}
