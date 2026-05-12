using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using System.Text.Json;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for generating AI-powered recommendations based on query results
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly ILogger<RecommendationService> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;

    public RecommendationService(
        IConfiguration configuration,
        ILogger<RecommendationService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["OpenAI:Endpoint"] ?? throw new ArgumentNullException("OpenAI:Endpoint");
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
        _deploymentName = configuration["OpenAI:DeploymentName"] ?? "gpt-4";
        _maxTokens = configuration.GetValue<int>("OpenAI:RecommendationMaxTokens", 1500);
        _temperature = configuration.GetValue<float>("OpenAI:RecommendationTemperature", 0.3f);

        _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<List<RecommendationDto>> GenerateRecommendationsAsync(
        string query, 
        QueryResult result, 
        string? schemaContext = null)
    {
        try
        {
            // Don't generate recommendations for empty results or very large datasets
            if (result.RowCount == 0)
            {
                _logger.LogInformation("No recommendations generated for empty result set");
                return new List<RecommendationDto>();
            }

            if (result.RowCount > 100)
            {
                _logger.LogInformation("Skipping recommendations for large result set ({RowCount} rows)", result.RowCount);
                return new List<RecommendationDto>();
            }

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(query, result, schemaContext);

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = _maxTokens,
                Temperature = _temperature,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            _logger.LogInformation("Generating recommendations for query: {Query}", query);

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var jsonResponse = response.Value.Choices[0].Message.Content;

            _logger.LogDebug("LLM Response: {Response}", jsonResponse);

            // Parse the JSON response
            var recommendations = ParseRecommendations(jsonResponse);

            _logger.LogInformation("Generated {Count} recommendations", recommendations.Count);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for query: {Query}", query);
            // Return empty list on error - don't fail the entire query
            return new List<RecommendationDto>();
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert business analyst and data strategist. 
Your role is to analyze database query results and provide actionable business insights.

Generate recommendations in three categories:
1. **RISKS**: Potential issues, concerns, or red flags in the data
2. **OPPORTUNITIES**: Positive trends, growth areas, or business opportunities
3. **ACTIONS**: Specific recommended next steps or follow-up queries

For each recommendation, provide:
- type: 'risk', 'opportunity', or 'action'
- title: Brief, clear title (max 50 chars)
- description: Detailed explanation (2-3 sentences)
- priority: 'high', 'medium', or 'low'
- impact: Business impact description
- effort: 'low', 'medium', or 'high' (for actions only)
- actions: Array of follow-up actions (optional)

Guidelines:
- Be specific and actionable
- Focus on business value
- Provide concrete numbers when relevant
- Suggest follow-up queries when appropriate
- Limit to 2-3 recommendations per category
- Only include high-value insights

Return valid JSON in this exact format:
{
  ""recommendations"": [
    {
      ""id"": ""unique-id"",
      ""type"": ""risk|opportunity|action"",
      ""title"": ""Brief title"",
      ""description"": ""Detailed explanation"",
      ""priority"": ""high|medium|low"",
      ""impact"": ""Impact description"",
      ""effort"": ""low|medium|high"",
      ""category"": ""Category name"",
      ""actions"": [
        {
          ""id"": ""action-id"",
          ""label"": ""Action label"",
          ""query"": ""Follow-up query text"",
          ""type"": ""query""
        }
      ]
    }
  ]
}";
    }

    private string BuildUserPrompt(string query, QueryResult result, string? schemaContext)
    {
        // Sample data (first 20 rows) to reduce token usage
        var sampleRows = result.Rows.Take(20).ToList();
        
        var dataJson = JsonSerializer.Serialize(new
        {
            columns = result.Columns,
            rows = sampleRows,
            totalRows = result.RowCount
        }, new JsonSerializerOptions { WriteIndented = false });

        var prompt = $@"Analyze this database query and provide business insights.

**User Query:** {query}

**Result Summary:**
- Columns: {string.Join(", ", result.Columns)}
- Total Rows: {result.RowCount}

**Sample Data:**
{dataJson}";

        if (!string.IsNullOrWhiteSpace(schemaContext))
        {
            prompt += $@"

**Database Context:**
{schemaContext}";
        }

        prompt += @"

Generate actionable recommendations (risks, opportunities, actions) based on this data.
Focus on business value and specific insights.
Return only valid JSON matching the specified format.";

        return prompt;
    }

    private List<RecommendationDto> ParseRecommendations(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var wrapper = JsonSerializer.Deserialize<RecommendationWrapper>(jsonResponse, options);
            
            if (wrapper?.Recommendations == null || wrapper.Recommendations.Count == 0)
            {
                _logger.LogWarning("No recommendations found in LLM response");
                return new List<RecommendationDto>();
            }

            // Ensure each recommendation has a valid ID
            foreach (var rec in wrapper.Recommendations)
            {
                if (string.IsNullOrEmpty(rec.Id))
                {
                    rec.Id = Guid.NewGuid().ToString("N").Substring(0, 12);
                }

                // Ensure actions have IDs
                if (rec.Actions != null)
                {
                    foreach (var action in rec.Actions)
                    {
                        if (string.IsNullOrEmpty(action.Id))
                        {
                            action.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                        }
                    }
                }
            }

            return wrapper.Recommendations;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse recommendation JSON: {Json}", jsonResponse);
            return new List<RecommendationDto>();
        }
    }

    private class RecommendationWrapper
    {
        public List<RecommendationDto> Recommendations { get; set; } = new();
    }
}
