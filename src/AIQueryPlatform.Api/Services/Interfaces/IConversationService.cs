using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Service for managing conversations and messages
/// </summary>
public interface IConversationService
{
    Task<ConversationListResponse> ListConversationsAsync(Guid tenantId, int page = 1, int pageSize = 20);
    Task<Conversation?> GetConversationAsync(string conversationId, Guid tenantId);
    Task<Conversation> CreateConversationAsync(string? title, Guid tenantId);
    Task<Message> AddMessageAsync(string conversationId, Guid tenantId, AddMessageRequest request);
    Task<bool> DeleteConversationAsync(string conversationId, Guid tenantId);
    Task<bool> PinConversationAsync(string conversationId, Guid tenantId, bool pinned);
    Task<bool> UpdateConversationTitleAsync(string conversationId, Guid tenantId, string title);
}
