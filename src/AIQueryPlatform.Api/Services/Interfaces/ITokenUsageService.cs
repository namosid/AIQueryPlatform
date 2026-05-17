using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Service for tracking and managing token usage
/// </summary>
public interface ITokenUsageService
{
    /// <summary>
    /// Get current token usage for a tenant
    /// </summary>
    Task<TokenUsageResponse> GetTokenUsageAsync(Guid tenantId);

    /// <summary>
    /// Record token usage for a request
    /// </summary>
    Task RecordTokenUsageAsync(RecordTokenUsageRequest request);

    /// <summary>
    /// Get daily usage trend for last N days
    /// </summary>
    Task<List<DailyUsageTrend>> GetDailyUsageTrendAsync(Guid tenantId, int days = 30);

    /// <summary>
    /// Get usage analytics
    /// </summary>
    Task<UsageAnalyticsResponse> GetUsageAnalyticsAsync(Guid tenantId);

    /// <summary>
    /// Get all tenants usage overview (admin)
    /// </summary>
    Task<List<TenantUsageOverview>> GetAllTenantsUsageAsync();

    /// <summary>
    /// Reset monthly usage for a tenant (admin)
    /// </summary>
    Task ResetMonthlyUsageAsync(Guid tenantId);

    /// <summary>
    /// Update tenant subscription
    /// </summary>
    Task UpdateSubscriptionAsync(Guid tenantId, UpdateSubscriptionRequest request);
}
