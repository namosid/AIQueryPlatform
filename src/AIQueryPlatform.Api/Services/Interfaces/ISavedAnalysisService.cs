using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Service for managing saved analyses
/// </summary>
public interface ISavedAnalysisService
{
    Task<SavedAnalysisListResponse> ListSavedAnalysesAsync(Guid tenantId);
    Task<SavedAnalysisDto?> GetSavedAnalysisAsync(string id, Guid tenantId);
    Task<SavedAnalysisDto> SaveAnalysisAsync(string conversationId, Guid tenantId, string title, string? description);
    Task<bool> DeleteSavedAnalysisAsync(string id, Guid tenantId);
}
