using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for converting natural language to SQL using OpenAI
/// </summary>
public class NLToSqlService : INLToSqlService
{
    private readonly ILogger<NLToSqlService> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;
    private readonly DatabasePromptBuilderFactory _promptBuilderFactory;

    public NLToSqlService(
        IConfiguration configuration,
        ILogger<NLToSqlService> logger,
        DatabasePromptBuilderFactory promptBuilderFactory)
    {
        _logger = logger;
        _promptBuilderFactory = promptBuilderFactory;
        
        var endpoint = configuration["OpenAI:Endpoint"] ?? throw new ArgumentNullException("OpenAI:Endpoint");
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
        _deploymentName = configuration["OpenAI:DeploymentName"] ?? "gpt-4";
        _maxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 500);
        _temperature = configuration.GetValue<float>("OpenAI:Temperature", 0.0f);

        _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<string> ConvertNaturalLanguageToSqlAsync(string query, DatabaseSchema schema, DatabaseType databaseType = DatabaseType.SqlServer)
    {
        try
        {
            // Get database-specific prompt builder
            var promptBuilder = _promptBuilderFactory.GetPromptBuilder(databaseType);
            var systemPrompt = promptBuilder.BuildSystemPrompt(schema);
            var userPrompt = query;

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = _maxTokens,
                Temperature = _temperature
            };

            _logger.LogInformation("Sending request to OpenAI for query: {Query} (Database: {DatabaseType})", query, databaseType);

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var sqlQuery = response.Value.Choices[0].Message.Content;

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
