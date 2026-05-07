using AIQueryPlatform.Api.Models;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface ITenantService
{
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
    Task<Tenant?> GetTenantByApiKeyAsync(string apiKey);
    Task<List<Tenant>> GetAllTenantsAsync();
}
