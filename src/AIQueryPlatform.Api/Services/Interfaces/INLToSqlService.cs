using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface INLToSqlService
{
    Task<string> ConvertNaturalLanguageToSqlAsync(string query, DatabaseSchema schema, DatabaseType databaseType = DatabaseType.SqlServer);
}
