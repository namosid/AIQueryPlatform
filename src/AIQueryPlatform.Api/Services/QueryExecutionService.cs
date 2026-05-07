using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for executing SQL queries against tenant databases with multi-database support
/// </summary>
public class QueryExecutionService : IQueryExecutionService
{
    private readonly ILogger<QueryExecutionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _queryTimeoutSeconds;

    public QueryExecutionService(
        IConfiguration configuration,
        ILogger<QueryExecutionService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _queryTimeoutSeconds = configuration.GetValue<int>("QueryExecution:QueryTimeoutSeconds", 30);
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, DatabaseType databaseType = DatabaseType.SqlServer)
    {
        try
        {
            _logger.LogInformation("Executing query for database type: {DatabaseType}", databaseType);

            // Get the appropriate database executor
            var executor = GetExecutor(databaseType);
            
            return await executor.ExecuteQueryAsync(sql, connectionString, _queryTimeoutSeconds);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution error for database type: {DatabaseType}", databaseType);
            throw new InvalidOperationException("Failed to execute query", ex);
        }
    }

    private IDatabaseExecutor GetExecutor(DatabaseType databaseType)
    {
        var executors = _serviceProvider.GetServices<IDatabaseExecutor>();
        var executor = executors.FirstOrDefault(e => e.SupportedDatabaseType == databaseType);

        if (executor == null)
        {
            throw new NotSupportedException($"Database type {databaseType} is not supported or the executor is not registered.");
        }

        return executor;
    }
}
