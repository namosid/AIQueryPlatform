using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for tracking and managing token usage
/// </summary>
public class TokenUsageService : ITokenUsageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenUsageService> _logger;
    private readonly string _connectionString;

    public TokenUsageService(
        IConfiguration configuration,
        ILogger<TokenUsageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("DefaultConnection");
    }

    public async Task<TokenUsageResponse> GetTokenUsageAsync(Guid tenantId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetTenantTokenUsage", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TenantId", tenantId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var billingCycleEnd = reader.GetDateTime(reader.GetOrdinal("BillingCycleEnd"));
                var daysUntilReset = (billingCycleEnd - DateTime.UtcNow).Days;
                var usedTokens = reader.GetInt64(reader.GetOrdinal("CurrentMonthUsedTokens"));
                
                var response = new TokenUsageResponse
                {
                    UsedTokens = usedTokens,
                    MonthlyLimit = reader.GetInt64(reader.GetOrdinal("MonthlyTokenLimit")),
                    RemainingTokens = reader.GetInt64(reader.GetOrdinal("RemainingTokens")),
                    UsagePercentage = reader.GetDecimal(reader.GetOrdinal("UsagePercentage")),
                    TotalRequests = reader.GetInt32(reader.GetOrdinal("TotalRequests")),
                    BillingCycleStart = reader.GetDateTime(reader.GetOrdinal("BillingCycleStart")),
                    BillingCycleEnd = billingCycleEnd,
                    PlanName = reader.GetString(reader.GetOrdinal("PlanName")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    DaysUntilReset = Math.Max(0, daysUntilReset),
                    EstimatedDailyUsage = CalculateEstimatedDailyUsage(usedTokens, DateTime.UtcNow, reader.GetDateTime(reader.GetOrdinal("BillingCycleStart"))),
                    LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"))
                };

                return response;
            }

            // No usage data found - return defaults
            return CreateDefaultUsageResponse(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token usage for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task RecordTokenUsageAsync(RecordTokenUsageRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_RecordTokenUsage", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@TenantId", request.TenantId);
            command.Parameters.AddWithValue("@ConversationId", (object?)request.ConversationId ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequestTokens", request.RequestTokens);
            command.Parameters.AddWithValue("@ResponseTokens", request.ResponseTokens);
            command.Parameters.AddWithValue("@TotalTokens", request.TotalTokens);
            command.Parameters.AddWithValue("@ModelName", request.ModelName);
            command.Parameters.AddWithValue("@Endpoint", (object?)request.Endpoint ?? DBNull.Value);
            command.Parameters.AddWithValue("@Query", (object?)request.Query ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", request.Status);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)request.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutionTimeMs", (object?)request.ExecutionTimeMs ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Recorded token usage for tenant {TenantId}: {TotalTokens} tokens (Request: {RequestTokens}, Response: {ResponseTokens})",
                request.TenantId, request.TotalTokens, request.RequestTokens, request.ResponseTokens
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording token usage for tenant {TenantId}", request.TenantId);
            // Don't throw - token recording failures shouldn't break the request
        }
    }

    public async Task<List<DailyUsageTrend>> GetDailyUsageTrendAsync(Guid tenantId, int days = 30)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    CAST(CreatedDate AS DATE) AS UsageDate,
                    SUM(TotalTokens) AS TotalTokens,
                    COUNT(*) AS RequestCount
                FROM TokenUsage
                WHERE TenantId = @TenantId
                    AND CreatedDate >= DATEADD(DAY, -@Days, GETUTCDATE())
                    AND Status = 'Success'
                GROUP BY CAST(CreatedDate AS DATE)
                ORDER BY UsageDate ASC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);
            command.Parameters.AddWithValue("@Days", days);

            var trends = new List<DailyUsageTrend>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                trends.Add(new DailyUsageTrend
                {
                    Date = reader.GetDateTime(0),
                    TotalTokens = reader.GetInt64(1),
                    RequestCount = reader.GetInt32(2)
                });
            }

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily usage trend for tenant {TenantId}", tenantId);
            return new List<DailyUsageTrend>();
        }
    }

    public async Task<UsageAnalyticsResponse> GetUsageAnalyticsAsync(Guid tenantId)
    {
        try
        {
            var dailyTrend = await GetDailyUsageTrendAsync(tenantId, 30);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get model breakdown
            var modelQuery = @"
                SELECT 
                    ModelName,
                    SUM(TotalTokens) AS TotalTokens
                FROM TokenUsage
                WHERE TenantId = @TenantId
                    AND CreatedDate >= DATEADD(DAY, -30, GETUTCDATE())
                    AND Status = 'Success'
                GROUP BY ModelName";

            using var modelCommand = new SqlCommand(modelQuery, connection);
            modelCommand.Parameters.AddWithValue("@TenantId", tenantId);

            var modelBreakdown = new Dictionary<string, long>();
            using var modelReader = await modelCommand.ExecuteReaderAsync();

            while (await modelReader.ReadAsync())
            {
                modelBreakdown[modelReader.GetString(0)] = modelReader.GetInt64(1);
            }

            var totalTokens = dailyTrend.Sum(t => t.TotalTokens);
            var totalRequests = dailyTrend.Sum(t => t.RequestCount);

            return new UsageAnalyticsResponse
            {
                DailyTrend = dailyTrend,
                ModelBreakdown = modelBreakdown,
                TotalTokensThisMonth = totalTokens,
                TotalRequestsThisMonth = totalRequests,
                AverageTokensPerRequest = totalRequests > 0 ? (decimal)totalTokens / totalRequests : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage analytics for tenant {TenantId}", tenantId);
            return new UsageAnalyticsResponse();
        }
    }

    public async Task<List<TenantUsageOverview>> GetAllTenantsUsageAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    t.TenantId,
                    t.Name AS TenantName,
                    sub.PlanName,
                    s.MonthlyTokenLimit,
                    s.CurrentMonthUsedTokens,
                    CAST(ROUND((CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0)) * 100, 2) AS DECIMAL(5,2)) AS UsagePercentage,
                    s.TotalRequests,
                    s.LastRequestDate,
                    CASE
                        WHEN s.CurrentMonthUsedTokens >= s.MonthlyTokenLimit THEN 'exceeded'
                        WHEN CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0) >= 0.95 THEN 'critical'
                        WHEN CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0) >= 0.80 THEN 'warning'
                        ELSE 'normal'
                    END AS Status
                FROM Tenants t
                LEFT JOIN TenantSubscriptions sub ON t.TenantId = sub.TenantId AND sub.IsActive = 1
                LEFT JOIN TokenUsageSummary s ON t.TenantId = s.TenantId
                ORDER BY s.CurrentMonthUsedTokens DESC";

            using var command = new SqlCommand(query, connection);

            var overviews = new List<TenantUsageOverview>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                overviews.Add(new TenantUsageOverview
                {
                    TenantId = reader.GetGuid(0),
                    TenantName = reader.GetString(1),
                    PlanName = reader.IsDBNull(2) ? "Free" : reader.GetString(2),
                    MonthlyLimit = reader.IsDBNull(3) ? 100000 : reader.GetInt64(3),
                    UsedTokens = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                    UsagePercentage = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    TotalRequests = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    LastRequestDate = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                    Status = reader.IsDBNull(8) ? "normal" : reader.GetString(8)
                });
            }

            return overviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants usage");
            return new List<TenantUsageOverview>();
        }
    }

    public async Task ResetMonthlyUsageAsync(Guid tenantId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE TokenUsageSummary
                SET CurrentMonthUsedTokens = 0,
                    RemainingTokens = MonthlyTokenLimit,
                    TotalRequests = 0,
                    SuccessfulRequests = 0,
                    FailedRequests = 0,
                    LastUpdated = GETUTCDATE()
                WHERE TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Reset monthly usage for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting monthly usage for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task UpdateSubscriptionAsync(Guid tenantId, UpdateSubscriptionRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE TenantSubscriptions
                SET PlanName = @PlanName,
                    MonthlyTokenLimit = @MonthlyTokenLimit,
                    UpdatedAt = GETUTCDATE()
                WHERE TenantId = @TenantId;

                UPDATE TokenUsageSummary
                SET MonthlyTokenLimit = @MonthlyTokenLimit,
                    RemainingTokens = @MonthlyTokenLimit - CurrentMonthUsedTokens,
                    LastUpdated = GETUTCDATE()
                WHERE TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);
            command.Parameters.AddWithValue("@PlanName", request.PlanName);
            command.Parameters.AddWithValue("@MonthlyTokenLimit", request.MonthlyTokenLimit);

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Updated subscription for tenant {TenantId} to {PlanName} with limit {MonthlyTokenLimit}",
                tenantId, request.PlanName, request.MonthlyTokenLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private TokenUsageResponse CreateDefaultUsageResponse(Guid tenantId)
    {
        return new TokenUsageResponse
        {
            MonthlyLimit = 100000,
            UsedTokens = 0,
            RemainingTokens = 100000,
            UsagePercentage = 0,
            Status = "normal",
            PlanName = "Free",
            TotalRequests = 0,
            BillingCycleStart = DateTime.UtcNow.AddDays(-DateTime.UtcNow.Day + 1).Date,
            BillingCycleEnd = DateTime.UtcNow.AddDays(-DateTime.UtcNow.Day + 1).AddMonths(1).AddDays(-1).Date,
            DaysUntilReset = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month) - DateTime.UtcNow.Day,
            EstimatedDailyUsage = 0,
            LastUpdated = DateTime.UtcNow
        };
    }

    private long CalculateEstimatedDailyUsage(long usedTokens, DateTime currentDate, DateTime billingCycleStart)
    {
        var daysElapsed = (currentDate - billingCycleStart).Days + 1;
        return daysElapsed > 0 ? usedTokens / daysElapsed : 0;
    }
}
