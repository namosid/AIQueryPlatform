using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Interface for building database-specific system prompts for NL to SQL conversion
/// </summary>
public interface IDatabasePromptBuilder
{
    /// <summary>
    /// Build a system prompt specific to the database type
    /// </summary>
    string BuildSystemPrompt(DatabaseSchema schema);
    
    /// <summary>
    /// Clean and format SQL response for the specific database
    /// </summary>
    string CleanSqlResponse(string sql);
    
    /// <summary>
    /// Get database-specific syntax rules and limitations
    /// </summary>
    string GetSyntaxRules();
}
