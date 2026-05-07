using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Interface for database-specific query executors
/// </summary>
public interface IDatabaseExecutor
{
    Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, int timeout = 30);
    DatabaseType SupportedDatabaseType { get; }
}
