using AIQueryPlatform.Api.Helpers;
using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for converting natural language to SQL using OpenAI
/// </summary>
public class NLToSqlService : INLToSqlService
{
    private readonly ILogger<NLToSqlService> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;
    private readonly DatabasePromptBuilderFactory _promptBuilderFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly TenantContext _tenantContext;

    public NLToSqlService(
        IConfiguration configuration,
        ILogger<NLToSqlService> logger,
        DatabasePromptBuilderFactory promptBuilderFactory,
        ITokenUsageService tokenUsageService,
        TenantContext tenantContext)
    {
        _logger = logger;
        _promptBuilderFactory = promptBuilderFactory;
        _tokenUsageService = tokenUsageService;
        _tenantContext = tenantContext;

        var endpoint = configuration["OpenAI:Endpoint"] ?? throw new ArgumentNullException("OpenAI:Endpoint");
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
        _deploymentName = configuration["OpenAI:DeploymentName"] ?? "gpt-4";
        _maxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 500);
        _temperature = configuration.GetValue<float>("OpenAI:Temperature", 0.0f);

       // _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<string> ConvertNaturalLanguageToSqlAsync(string query, DatabaseSchema schema, DatabaseType databaseType = DatabaseType.SqlServer)
    {
        try
        {
            // Get database-specific prompt builder
            var promptBuilder = _promptBuilderFactory.GetPromptBuilder(databaseType);
            var systemPrompt = promptBuilder.BuildSystemPrompt(schema);
            var userPrompt = query;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _maxTokens,
                Temperature = (float)_temperature
            };
            _logger.LogInformation("Sending request to OpenAI for query: {Query} (Database: {DatabaseType})", query, databaseType);

            var _chatClient = _openAIClient.GetChatClient(_deploymentName);

            var response = await _chatClient.CompleteChatAsync(
                messages,
                chatCompletionOptions);
            var sqlQuery = response.Value.Content[0].Text;

            //// Track token usage
            //if (_tenantContext.HasTenant && response.Value.HasTokenUsage())
            //{
            //    var (promptTokens, completionTokens, totalTokens) = response.Value.ExtractTokenUsage();
            //    await _tokenUsageService.RecordTokenUsageAsync(new RecordTokenUsageRequest
            //    {
            //        TenantId = _tenantContext.CurrentTenant!.TenantId,
            //        RequestTokens = promptTokens,
            //        ResponseTokens = completionTokens,
            //        TotalTokens = totalTokens,
            //        ModelName = _deploymentName,
            //        Endpoint = "NL-to-SQL",
            //        Query = query,
            //        Status = "Success"
            //    });
            //}

            // Clean up the response using database-specific cleaner
            sqlQuery = promptBuilder.CleanSqlResponse(sqlQuery);

            // Validate SQL syntax
            if (!ValidateSqlSyntax(sqlQuery, out var syntaxError))
            {
                _logger.LogWarning("Generated SQL has syntax issues: {Error}. Attempting to fix...", syntaxError);
            }

            _logger.LogInformation("Generated SQL: {Sql}", sqlQuery);

            return sqlQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting natural language to SQL");
            throw new InvalidOperationException("Failed to generate SQL query", ex);
        }
    }

    private bool ValidateSqlSyntax(string sql, out string? errorMessage)
    {
        errorMessage = null;

        // Check for balanced parentheses
        var openParens = 0;
        var lineNumber = 1;
        var charPosition = 0;

        foreach (var c in sql)
        {
            charPosition++;
            if (c == '\n')
            {
                lineNumber++;
                charPosition = 0;
            }

            if (c == '(') openParens++;
            if (c == ')')
            {
                openParens--;
                if (openParens < 0)
                {
                    errorMessage = $"Unbalanced parentheses: extra closing parenthesis at line {lineNumber}, position {charPosition}";
                    return false;
                }
            }
        }

        if (openParens != 0)
        {
            errorMessage = $"Unbalanced parentheses: {Math.Abs(openParens)} unclosed opening parenthesis(es)";
            return false;
        }

        // Check for basic SQL syntax requirements
        if (!sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !sql.Trim().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Query must start with SELECT or WITH (for CTEs)";
            return false;
        }

        return true;
    }
}
