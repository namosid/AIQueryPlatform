namespace AIQueryPlatform.Api.Models.DTOs;

/// <summary>
/// Conversation entity
/// </summary>
public class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<Message> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPinned { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Message entity within a conversation
/// </summary>
public class Message
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public QueryResult? Data { get; set; }
    public ChartData? ChartData { get; set; }
    public List<RecommendationDto>? Recommendations { get; set; }
}

/// <summary>
/// Request to create a new conversation
/// </summary>
public class CreateConversationRequest
{
    public string? Title { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Request to add a message to a conversation
/// </summary>
public class AddMessageRequest
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public QueryResult? Data { get; set; }
    public ChartData? ChartData { get; set; }
}

/// <summary>
/// Response for conversation list
/// </summary>
public class ConversationListResponse
{
    public List<Conversation> Conversations { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Request to update conversation pin status
/// </summary>
public class PinConversationRequest
{
    public bool Pinned { get; set; }
}

/// <summary>
/// Request to execute a query within a conversation
/// </summary>
public class ConversationQueryRequest
{
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Response from executing a query in a conversation
/// </summary>
public class ConversationQueryResponse
{
    public Message UserMessage { get; set; } = null!;
    public Message AssistantMessage { get; set; } = null!;
    public QueryResponse QueryResult { get; set; } = null!;
}

/// <summary>
/// Saved Analysis entity
/// </summary>
public class SavedAnalysisDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime SavedAt { get; set; }
}

/// <summary>
/// Request to save an analysis
/// </summary>
public class SaveAnalysisRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Response for saved analyses list
/// </summary>
public class SavedAnalysisListResponse
{
    public List<SavedAnalysisDto> Analyses { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>
/// AI-generated recommendation for query results
/// </summary>
public class RecommendationDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "action"; // "risk", "opportunity", "action"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // "high", "medium", "low"
    public string? Impact { get; set; }
    public string? Effort { get; set; } // "low", "medium", "high"
    public string? Category { get; set; }
    public List<RecommendedActionDto>? Actions { get; set; }
}

/// <summary>
/// Recommended action within a recommendation
/// </summary>
public class RecommendedActionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Query { get; set; } // Follow-up query to execute
    public string? Url { get; set; } // External link
    public string Type { get; set; } = "query"; // "query", "link", "export"
}
