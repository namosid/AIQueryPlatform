namespace AIQueryPlatform.Api.Models.DTOs;

/// <summary>
/// Tenant subscription/plan details
/// </summary>
public class TenantSubscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PlanName { get; set; } = "Free";
    public long MonthlyTokenLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Detailed token usage record
/// </summary>
public class TokenUsageRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? ConversationId { get; set; }
    
    // Token counts
    public int RequestTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int TotalTokens { get; set; }
    
    // Model info
    public string ModelName { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    
    // Request metadata
    public string? Query { get; set; }
    public string Status { get; set; } = "Success";
    public string? ErrorMessage { get; set; }
    
    // Timestamps
    public DateTime CreatedDate { get; set; }
    
    // Performance
    public int? ExecutionTimeMs { get; set; }
}

/// <summary>
/// Token usage summary for a tenant
/// </summary>
public class TokenUsageSummaryDto
{
    public Guid TenantId { get; set; }
    
    // Billing cycle
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd { get; set; }
    
    // Token counts
    public long CurrentMonthUsedTokens { get; set; }
    public long MonthlyTokenLimit { get; set; }
    public long RemainingTokens { get; set; }
    
    // Statistics
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    
    // Metadata
    public DateTime LastUpdated { get; set; }
    public DateTime? LastRequestDate { get; set; }
}

/// <summary>
/// Token usage response for API
/// </summary>
public class TokenUsageResponse
{
    public long MonthlyLimit { get; set; }
    public long UsedTokens { get; set; }
    public long RemainingTokens { get; set; }
    public decimal UsagePercentage { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, critical, exceeded
    public string PlanName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd { get; set; }
    public int DaysUntilReset { get; set; }
    public long EstimatedDailyUsage { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Quota validation result
/// </summary>
public class QuotaValidationResult
{
    public bool HasQuota { get; set; }
    public long RemainingTokens { get; set; }
    public decimal UsagePercentage { get; set; }
    public string Status { get; set; } = "normal";
    public string? Message { get; set; }
    public QuotaErrorCode? ErrorCode { get; set; }
}

/// <summary>
/// Quota error codes
/// </summary>
public enum QuotaErrorCode
{
    None,
    TokenLimitExceeded,
    SubscriptionInactive,
    SubscriptionNotFound
}

/// <summary>
/// Request to record token usage
/// </summary>
public class RecordTokenUsageRequest
{
    public Guid TenantId { get; set; }
    public string? ConversationId { get; set; }
    public int RequestTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int TotalTokens { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Query { get; set; }
    public string Status { get; set; } = "Success";
    public string? ErrorMessage { get; set; }
    public int? ExecutionTimeMs { get; set; }
}

/// <summary>
/// Daily usage trend data point
/// </summary>
public class DailyUsageTrend
{
    public DateTime Date { get; set; }
    public long TotalTokens { get; set; }
    public int RequestCount { get; set; }
}

/// <summary>
/// Usage analytics response
/// </summary>
public class UsageAnalyticsResponse
{
    public List<DailyUsageTrend> DailyTrend { get; set; } = new();
    public Dictionary<string, long> ModelBreakdown { get; set; } = new();
    public long TotalTokensThisMonth { get; set; }
    public int TotalRequestsThisMonth { get; set; }
    public decimal AverageTokensPerRequest { get; set; }
}

/// <summary>
/// Request to update tenant subscription
/// </summary>
public class UpdateSubscriptionRequest
{
    public string PlanName { get; set; } = string.Empty;
    public long MonthlyTokenLimit { get; set; }
}

/// <summary>
/// Admin: Tenant usage overview
/// </summary>
public class TenantUsageOverview
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public long MonthlyLimit { get; set; }
    public long UsedTokens { get; set; }
    public decimal UsagePercentage { get; set; }
    public int TotalRequests { get; set; }
    public DateTime LastRequestDate { get; set; }
    public string Status { get; set; } = "normal";
}
