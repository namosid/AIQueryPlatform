using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface ISchemaService
{
    Task<DatabaseSchema> GetDatabaseSchemaAsync(string connectionString, Guid tenantId);
    Task InvalidateCacheAsync(Guid tenantId);
}
