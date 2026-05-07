using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface IReportingService
{
    Task<byte[]> GeneratePdfReportAsync(
        QueryResult result, 
        string reportTitle, 
        Tenant tenant,
        ChartData? chartData = null);
}
