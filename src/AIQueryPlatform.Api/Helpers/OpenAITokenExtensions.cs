using Azure.AI.OpenAI;

namespace AIQueryPlatform.Api.Helpers;

/// <summary>
/// Helper extensions for extracting token usage from OpenAI responses
/// </summary>
public static class OpenAITokenExtensions
{
    /// <summary>
    /// Extract token usage from OpenAI ChatCompletions response
    /// </summary>
    public static (int promptTokens, int completionTokens, int totalTokens) ExtractTokenUsage(
        this ChatCompletions response)
    {
        var usage = response.Usage;
        
        return (
            promptTokens: usage.PromptTokens,
            completionTokens: usage.CompletionTokens,
            totalTokens: usage.TotalTokens
        );
    }

    /// <summary>
    /// Check if response contains token usage information
    /// </summary>
    public static bool HasTokenUsage(this ChatCompletions response)
    {
        return response.Usage != null;
    }
}
