using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface IQueryOrchestrationService
{
    IAsyncEnumerable<StreamEvent> ExecuteQueryStreamAsync(string query);
    Task<QueryResponse> ExecuteQueryAsync(string query);
}
