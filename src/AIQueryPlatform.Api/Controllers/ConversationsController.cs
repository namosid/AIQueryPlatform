using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIQueryPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IQueryOrchestrationService _queryOrchestrationService;
    private readonly IRecommendationService _recommendationService;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationService conversationService,
        IQueryOrchestrationService queryOrchestrationService,
        IRecommendationService recommendationService,
        TenantContext tenantContext,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _queryOrchestrationService = queryOrchestrationService;
        _recommendationService = recommendationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// List all conversations for the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConversationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConversationListResponse>> ListConversationsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var response = await _conversationService.ListConversationsAsync(tenantId, page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversations for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to list conversations" });
        }
    }

    /// <summary>
    /// Get a specific conversation by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Conversation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Conversation>> GetConversationAsync(string id)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var conversation = await _conversationService.GetConversationAsync(id, tenantId);

            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to get conversation" });
        }
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Conversation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Conversation>> CreateConversationAsync(
        [FromBody] CreateConversationRequest? request)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var conversation = await _conversationService.CreateConversationAsync(
                request?.Title,
                tenantId
            );

            // Return 201 Created with the conversation object
            return StatusCode(201, conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to create conversation" });
        }
    }

    /// <summary>
    /// Add a message to a conversation
    /// </summary>
    [HttpPost("{id}/messages")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Message>> AddMessageAsync(
        string id,
        [FromBody] AddMessageRequest request)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Message content cannot be empty" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var message = await _conversationService.AddMessageAsync(id, tenantId, request);
            return CreatedAtAction(
                nameof(GetConversationAsync),
                new { id = id },
                message
            );
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to add message" });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteConversationAsync(string id)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var deleted = await _conversationService.DeleteConversationAsync(id, tenantId);

            if (!deleted)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to delete conversation" });
        }
    }

    /// <summary>
    /// Pin or unpin a conversation
    /// </summary>
    [HttpPatch("{id}/pin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PinConversationAsync(
        string id,
        [FromBody] PinConversationRequest request)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var updated = await _conversationService.PinConversationAsync(id, tenantId, request.Pinned);

            if (!updated)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            return Ok(new { success = true, pinned = request.Pinned });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to update conversation" });
        }
    }

    /// <summary>
    /// Execute a query within a conversation (unified endpoint)
    /// This endpoint:
    /// 1. Saves the user's query message
    /// 2. Executes the query via LLM (NL to SQL conversion)
    /// 3. Saves the AI assistant's response with results
    /// 4. Returns both messages and the query results
    /// </summary>
    [HttpPost("{id}/query")]
    [ProducesResponseType(typeof(ConversationQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConversationQueryResponse>> ExecuteQueryInConversationAsync(
        string id,
        [FromBody] ConversationQueryRequest request)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query cannot be empty" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            _logger.LogInformation("Executing query in conversation {ConversationId}: {Query}", id, request.Query);

            // Step 1: Save user message
            var userMessage = await _conversationService.AddMessageAsync(id, tenantId, new AddMessageRequest
            {
                Role = "user",
                Content = request.Query,
                Data = null,
                ChartData = null
            });

            _logger.LogInformation("User message saved: {MessageId}", userMessage.Id);

            // Step 2: Execute query via LLM
            var queryResponse = await _queryOrchestrationService.ExecuteQueryAsync(request.Query);

            if (!queryResponse.Success)
            {
                _logger.LogWarning("Query execution failed: {ErrorMessage}", queryResponse.ErrorMessage);

                // Save error message from AI
                var errorMessage = await _conversationService.AddMessageAsync(id, tenantId, new AddMessageRequest
                {
                    Role = "assistant",
                    Content = $"I encountered an error while processing your query: {queryResponse.ErrorMessage}",
                    Data = null,
                    ChartData = null
                });

                return Ok(new ConversationQueryResponse
                {
                    UserMessage = userMessage,
                    AssistantMessage = errorMessage,
                    QueryResult = queryResponse
                });
            }

            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", queryResponse.Result?.RowCount ?? 0);

            // Step 3: Generate AI recommendations asynchronously (only if enabled for tenant)
            List<RecommendationDto>? recommendations = null;
            
            // Check if insights/recommendations are enabled for this tenant
            if (_tenantContext.CurrentTenant!.EnableInsights)
            {
                try
                {
                    if (queryResponse.Result != null && queryResponse.Result.RowCount > 0)
                    {
                        _logger.LogInformation("Generating AI recommendations for query results (insights enabled for tenant)");
                        recommendations = await _recommendationService.GenerateRecommendationsAsync(
                            request.Query,
                            queryResponse.Result,
                            schemaContext: null // TODO: Add schema context if available
                        );
                        _logger.LogInformation("Generated {Count} recommendations", recommendations?.Count ?? 0);
                    }
                }
                catch (Exception recEx)
                {
                    _logger.LogWarning(recEx, "Failed to generate recommendations, continuing without them");
                    // Don't fail the entire request if recommendations fail
                }
            }
            else
            {
                _logger.LogInformation("Skipping recommendations generation - insights disabled for tenant {TenantId}", tenantId);
            }

            // Step 4: Generate AI response text
            var assistantContent = GenerateAssistantResponse(request.Query, queryResponse);

            // Step 5: Save AI assistant message with results and recommendations
            var assistantMessage = await _conversationService.AddMessageAsync(id, tenantId, new AddMessageRequest
            {
                Role = "assistant",
                Content = assistantContent,
                Data = queryResponse.Result,
                ChartData = queryResponse.ChartData
            });

            // Add recommendations to the message if generated
            if (recommendations != null && recommendations.Count > 0)
            {
                assistantMessage.Recommendations = recommendations;
            }

            _logger.LogInformation("Assistant message saved: {MessageId}", assistantMessage.Id);

            // Step 6: Update conversation title if it's the first query
            var conversation = await _conversationService.GetConversationAsync(id, tenantId);
            if (conversation != null && conversation.Title == "New Conversation" && conversation.Messages.Count <= 2)
            {
                // Generate a meaningful title from the query
                var generatedTitle = GenerateConversationTitle(request.Query);
                await _conversationService.UpdateConversationTitleAsync(id, tenantId, generatedTitle);
                _logger.LogInformation("Updated conversation {ConversationId} title to: {Title}", id, generatedTitle);
            }

            // Step 7: Return complete response
            return Ok(new ConversationQueryResponse
            {
                UserMessage = userMessage,
                AssistantMessage = assistantMessage,
                QueryResult = queryResponse
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query in conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to execute query" });
        }
    }

    /// <summary>
    /// Generate a natural language response from the AI assistant
    /// </summary>
    private string GenerateAssistantResponse(string query, QueryResponse queryResponse)
    {
        if (queryResponse.Result == null)
        {
            return "I couldn't generate results for your query.";
        }

        var rowCount = queryResponse.Result.RowCount;

        // Generate contextual response based on the query and results
        var response = $"I found {rowCount} result{(rowCount != 1 ? "s" : "")} for your query";

        if (queryResponse.ExecutionTimeMs > 0)
        {
            response += $" (executed in {queryResponse.ExecutionTimeMs}ms)";
        }

        response += ". ";

        // Add visualization type info
        if (queryResponse.VisualizationType == VisualizationType.Chart && queryResponse.ChartData != null)
        {
            response += $"I've prepared a {queryResponse.ChartData.ChartType} chart to visualize the data. ";
        }
        else if (queryResponse.VisualizationType == VisualizationType.Table)
        {
            response += "The results are displayed in a table below. ";
        }

        // Add insight based on row count
        if (rowCount == 0)
        {
            response = "Your query executed successfully, but returned no results. You might want to adjust your criteria.";
        }
        else if (rowCount >= 100)
        {
            response += "Note: Results are limited to 100 rows for performance.";
        }

        return response;
    }

    /// <summary>
    /// Generate a meaningful conversation title from the first query
    /// </summary>
    private string GenerateConversationTitle(string query)
    {
        // Trim and limit length
        var title = query.Trim();
        
        // Capitalize first letter
        if (title.Length > 0)
        {
            title = char.ToUpper(title[0]) + title.Substring(1);
        }
        
        // Truncate if too long and add ellipsis
        const int maxLength = 50;
        if (title.Length > maxLength)
        {
            title = title.Substring(0, maxLength).TrimEnd() + "...";
        }
        
        return title;
    }
}
