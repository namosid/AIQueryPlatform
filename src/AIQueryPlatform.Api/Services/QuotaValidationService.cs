using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for validating tenant quota before AI requests
/// </summary>
public class QuotaValidationService : IQuotaValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuotaValidationService> _logger;
    private readonly string _connectionString;

    public QuotaValidationService(
        IConfiguration configuration,
        ILogger<QuotaValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("DefaultConnection");
    }

    public async Task<QuotaValidationResult> ValidateQuotaAsync(Guid tenantId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_CheckTenantQuota", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@TenantId", tenantId);
            
            var hasQuotaParam = new SqlParameter("@HasQuota", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var remainingTokensParam = new SqlParameter("@RemainingTokens", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
            var usagePercentageParam = new SqlParameter("@UsagePercentage", SqlDbType.Decimal) 
            { 
                Direction = ParameterDirection.Output,
                Precision = 5,
                Scale = 2
            };

            command.Parameters.Add(hasQuotaParam);
            command.Parameters.Add(remainingTokensParam);
            command.Parameters.Add(usagePercentageParam);

            await command.ExecuteNonQueryAsync();

            var hasQuota = (bool)(hasQuotaParam.Value ?? true);
            var remainingTokens = (long)(remainingTokensParam.Value ?? 100000L);
            var usagePercentage = (decimal)(usagePercentageParam.Value ?? 0m);

            var status = DetermineQuotaStatus(usagePercentage);

            var result = new QuotaValidationResult
            {
                HasQuota = hasQuota,
                RemainingTokens = remainingTokens,
                UsagePercentage = usagePercentage,
                Status = status
            };

            if (!hasQuota)
            {
                result.ErrorCode = QuotaErrorCode.TokenLimitExceeded;
                result.Message = "Monthly AI token quota exceeded. Please upgrade your plan to continue using AI features.";
                _logger.LogWarning("Quota exceeded for tenant {TenantId}. Usage: {UsagePercentage}%", tenantId, usagePercentage);
            }
            else if (usagePercentage >= 95)
            {
                result.Message = $"Critical: You have used {usagePercentage:F1}% of your monthly AI quota. Only {remainingTokens:N0} tokens remaining.";
                _logger.LogWarning("Critical quota level for tenant {TenantId}. Usage: {UsagePercentage}%", tenantId, usagePercentage);
            }
            else if (usagePercentage >= 80)
            {
                result.Message = $"Warning: You have used {usagePercentage:F1}% of your monthly AI quota. {remainingTokens:N0} tokens remaining.";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating quota for tenant {TenantId}", tenantId);
            
            // On error, allow the request but log it
            return new QuotaValidationResult
            {
                HasQuota = true,
                RemainingTokens = 0,
                UsagePercentage = 0,
                Status = "normal",
                Message = "Unable to validate quota. Request allowed."
            };
        }
    }

    public async Task<QuotaValidationResult> ValidateQuotaWithEstimateAsync(Guid tenantId, int estimatedTokens)
    {
        var result = await ValidateQuotaAsync(tenantId);

        if (result.HasQuota && result.RemainingTokens < estimatedTokens)
        {
            result.HasQuota = false;
            result.ErrorCode = QuotaErrorCode.TokenLimitExceeded;
            result.Message = $"Insufficient quota. This request requires approximately {estimatedTokens} tokens, but only {result.RemainingTokens} remaining.";
            _logger.LogWarning(
                "Insufficient quota for tenant {TenantId}. Estimated: {EstimatedTokens}, Remaining: {RemainingTokens}",
                tenantId, estimatedTokens, result.RemainingTokens
            );
        }

        return result;
    }

    public async Task<string> GetQuotaStatusAsync(Guid tenantId)
    {
        var result = await ValidateQuotaAsync(tenantId);
        return result.Status;
    }

    private string DetermineQuotaStatus(decimal usagePercentage)
    {
        if (usagePercentage >= 100)
            return "exceeded";
        if (usagePercentage >= 95)
            return "critical";
        if (usagePercentage >= 80)
            return "warning";
        return "normal";
    }
}
