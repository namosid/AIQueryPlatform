using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for managing tenant data from the master database
/// </summary>
public class TenantService : ITenantService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TenantService(
        IMemoryCache cache, 
        ILogger<TenantService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        var cacheKey = $"tenant_id_{tenantId}";
        
        if (_cache.TryGetValue<Tenant>(cacheKey, out var cachedTenant) && cachedTenant != null)
        {
            return cachedTenant;
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TenantId, Name, ApiKey, ConnectionString, LogoUrl, ThemeColor, IsActive, EnableInsights
                FROM Tenants
                WHERE TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var tenant = new Tenant
                {
                    TenantId = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    ApiKey = reader.GetString(2),
                    ConnectionString = reader.GetString(3),
                    LogoUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ThemeColor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsActive = reader.GetBoolean(6),
                    EnableInsights = reader.GetBoolean(7)
                };

                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogInformation("Loaded tenant {TenantId} from database", tenantId);
                
                return tenant;
            }

            _logger.LogWarning("Tenant {TenantId} not found in database", tenantId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant {TenantId} from database", tenantId);
            throw;
        }
    }

    public async Task<Tenant?> GetTenantByApiKeyAsync(string apiKey)
    {
        var cacheKey = $"tenant_key_{apiKey}";
        
        if (_cache.TryGetValue<Tenant>(cacheKey, out var cachedTenant) && cachedTenant != null)
        {
            return cachedTenant;
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TenantId, Name, ApiKey, ConnectionString, LogoUrl, ThemeColor, IsActive, EnableInsights,SchemaFile
                FROM Tenants
                WHERE ApiKey = @ApiKey";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ApiKey", apiKey);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var tenant = new Tenant
                {
                    TenantId = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    ApiKey = reader.GetString(2),
                    ConnectionString = reader.GetString(3),
                    LogoUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ThemeColor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsActive = reader.GetBoolean(6),
                    EnableInsights = reader.GetBoolean(7),
                    SchemaFile = reader.IsDBNull(8) ? null : reader.GetString(8)
                };

                _cache.Set(cacheKey, tenant, CacheDuration);
                // Also cache by TenantId for faster lookups
                _cache.Set($"tenant_id_{tenant.TenantId}", tenant, CacheDuration);
                
                _logger.LogInformation("Loaded tenant with API key from database");
                
                return tenant;
            }

            _logger.LogWarning("Tenant with API key not found in database");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant by API key from database");
            throw;
        }
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        const string cacheKey = "all_active_tenants";
        
        if (_cache.TryGetValue<List<Tenant>>(cacheKey, out var cachedTenants) && cachedTenants != null)
        {
            return cachedTenants;
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TenantId, Name, ApiKey, ConnectionString, LogoUrl, ThemeColor, IsActive, EnableInsights,SchemaFile
                FROM Tenants
                WHERE IsActive = 1
                ORDER BY Name";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var tenants = new List<Tenant>();
            
            while (await reader.ReadAsync())
            {
                var tenant = new Tenant
                {
                    TenantId = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    ApiKey = reader.GetString(2),
                    ConnectionString = reader.GetString(3),
                    LogoUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ThemeColor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsActive = reader.GetBoolean(6),
                    EnableInsights = reader.GetBoolean(7),
                    SchemaFile = reader.IsDBNull(8) ? null : reader.GetString(8)
                };

                tenants.Add(tenant);
            }

            _cache.Set(cacheKey, tenants, CacheDuration);
            _logger.LogInformation("Loaded {Count} active tenants from database", tenants.Count);
            
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all tenants from database");
            throw;
        }
    }

    public async Task UpdateInsightsSettingAsync(Guid tenantId, bool enableInsights)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Tenants
                SET EnableInsights = @EnableInsights,
                    UpdatedAt = GETUTCDATE()
                WHERE TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);
            command.Parameters.AddWithValue("@EnableInsights", enableInsights);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Invalidate cache
                _cache.Remove($"tenant_id_{tenantId}");
                _cache.Remove("all_active_tenants");
                
                _logger.LogInformation("Updated insights setting for tenant {TenantId}: {Enabled}", 
                    tenantId, enableInsights);
            }
            else
            {
                _logger.LogWarning("No tenant found with ID {TenantId} to update insights setting", tenantId);
                throw new InvalidOperationException("Tenant not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating insights setting for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
