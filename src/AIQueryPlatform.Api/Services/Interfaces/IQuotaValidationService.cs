using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Service for validating tenant quota before AI requests
/// </summary>
public interface IQuotaValidationService
{
    /// <summary>
    /// Check if tenant has available quota
    /// </summary>
    Task<QuotaValidationResult> ValidateQuotaAsync(Guid tenantId);

    /// <summary>
    /// Check if tenant has enough quota for estimated tokens
    /// </summary>
    Task<QuotaValidationResult> ValidateQuotaWithEstimateAsync(Guid tenantId, int estimatedTokens);

    /// <summary>
    /// Get quota status (normal, warning, critical, exceeded)
    /// </summary>
    Task<string> GetQuotaStatusAsync(Guid tenantId);
}
