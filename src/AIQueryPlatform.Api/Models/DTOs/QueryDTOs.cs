namespace AIQueryPlatform.Api.Models.DTOs;

/// <summary>
/// Request DTO for natural language query
/// </summary>
public class QueryRequest
{
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for query execution
/// </summary>
public class QueryResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string GeneratedSql { get; set; } = string.Empty;
    public QueryResult? Result { get; set; }
    public VisualizationType VisualizationType { get; set; }
    public ChartData? ChartData { get; set; }
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Query result with columns and rows
/// </summary>
public class QueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
}

/// <summary>
/// Streaming event types
/// </summary>
public class StreamEvent
{
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Sql { get; set; }
    public QueryResult? Data { get; set; }
    public VisualizationType? VisualizationType { get; set; }
    public ChartData? ChartData { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Visualization type determined by Intelligence Layer
/// </summary>
public enum VisualizationType
{
    Table,
    Chart,
    PDF
}

/// <summary>
/// Chart data format supporting multi-series
/// </summary>
public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
    public string ChartType { get; set; } = "bar";
}

/// <summary>
/// Dataset for multi-series charts
/// </summary>
public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public List<double> Data { get; set; } = new();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
}

/// <summary>
/// Report request DTO
/// </summary>
public class ReportRequest
{
    public string Query { get; set; } = string.Empty;
    public string? ReportTitle { get; set; }
}
