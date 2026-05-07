using AIQueryPlatform.Api.Models.DTOs;

namespace AIQueryPlatform.Api.Services.Interfaces;

public interface IIntelligenceLayerService
{
    VisualizationType DetermineVisualizationType(string query, QueryResult result);
    ChartData? ConvertToChartData(QueryResult result);
}
