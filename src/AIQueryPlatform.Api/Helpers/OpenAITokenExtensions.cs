using Azure.AI.OpenAI;
using OpenAI.Chat;

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
        this ChatCompletion response)
    {
        var usage = response.Usage;
        
        return (
            promptTokens: usage.InputTokenCount,
            completionTokens: usage.OutputTokenCount,
            totalTokens: usage.TotalTokenCount
        );
    }

    /// <summary>
    /// Check if response contains token usage information
    /// </summary>
    public static bool HasTokenUsage(this ChatCompletion response)
    {
        return response.Usage != null;
    }
}
