using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

/// <summary>
/// Service for generating AI-powered recommendations based on query results
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generate recommendations (risks, opportunities, actions) based on query results
    /// </summary>
    /// <param name="query">The natural language query from the user</param>
    /// <param name="result">The query execution result</param>
    /// <param name="schemaContext">Database schema context for better insights</param>
    /// <returns>List of AI-generated recommendations</returns>
    Task<List<RecommendationDto>> GenerateRecommendationsAsync(
        string query, 
        QueryResult result, 
        string? schemaContext = null);
}
