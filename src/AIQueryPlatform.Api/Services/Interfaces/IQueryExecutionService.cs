using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface IQueryExecutionService
{
    Task<QueryResult> ExecuteQueryAsync(string sql, string connectionString, DatabaseType databaseType = DatabaseType.SqlServer);
}
