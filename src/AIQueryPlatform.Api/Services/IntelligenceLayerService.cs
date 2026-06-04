using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for determining visualization type and transforming data
/// </summary>
public class IntelligenceLayerService : IIntelligenceLayerService
{
    private readonly ILogger<IntelligenceLayerService> _logger;

    // Keywords that suggest report/PDF generation
    private static readonly HashSet<string> ReportKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "report", "summary", "export", "generate", "download", "pdf"
    };

    public IntelligenceLayerService(ILogger<IntelligenceLayerService> logger)
    {
        _logger = logger;
    }

    public VisualizationType DetermineVisualizationType(string query, QueryResult result)
    {
        // Check if query contains report keywords
        foreach (var keyword in ReportKeywords)
        {
            if (Regex.IsMatch(query, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("Detected report keyword '{Keyword}' - suggesting PDF", keyword);
                return VisualizationType.PDF;
            }
        }

        // Check if result is suitable for chart
        if (IsChartSuitable(result))
        {
            _logger.LogInformation("Data suitable for chart visualization");
            return VisualizationType.Chart;
        }

        // Default to table
        _logger.LogInformation("Using default table visualization");
        return VisualizationType.Table;
    }

    public ChartData? ConvertToChartData(QueryResult result)
    {
        if (!IsChartSuitable(result))
        {
            return null;
        }

        try
        {
            var chartData = new ChartData();

            // Detect chart structure based on columns
            if (result.Columns.Count == 2)
            {
                // Simple 2-column chart: labels and values
                return CreateSimpleChart(result);
            }
            else if (result.Columns.Count >= 3)
            {
                // Multi-series chart: first column as labels, rest as series
                return CreateMultiSeriesChart(result);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert data to chart format");
            return null;
        }
    }

    private ChartData CreateSimpleChart(QueryResult result)
    {
        var chartData = new ChartData();
        var labelColumn = result.Columns[0];
        var valueColumn = result.Columns[1];

        var dataset = new ChartDataset
        {
            Label = valueColumn,
            BackgroundColor = "rgba(54, 162, 235, 0.6)",
            BorderColor = "rgba(54, 162, 235, 1)"
        };

        foreach (var row in result.Rows)
        {
            var label = row[labelColumn]?.ToString() ?? "Unknown";
            chartData.Labels.Add(label);

            var value = ConvertToDouble(row[valueColumn]);
            dataset.Data.Add(value);
        }

        chartData.Datasets.Add(dataset);
        chartData.ChartType = DetermineChartType(result);

        _logger.LogInformation("Created simple chart with {RowCount} data points", result.RowCount);
        return chartData;
    }

    private ChartData CreateMultiSeriesChart(QueryResult result)
    {
        var chartData = new ChartData();
        var labelColumn = result.Columns[0];
        
        // Color palette for multiple series
        var colors = new[]
        {
            ("rgba(54, 162, 235, 0.6)", "rgba(54, 162, 235, 1)"),
            ("rgba(255, 99, 132, 0.6)", "rgba(255, 99, 132, 1)"),
            ("rgba(75, 192, 192, 0.6)", "rgba(75, 192, 192, 1)"),
            ("rgba(255, 206, 86, 0.6)", "rgba(255, 206, 86, 1)"),
            ("rgba(153, 102, 255, 0.6)", "rgba(153, 102, 255, 1)"),
            ("rgba(255, 159, 64, 0.6)", "rgba(255, 159, 64, 1)")
        };

        // Extract labels from first column
        foreach (var row in result.Rows)
        {
            var label = row[labelColumn]?.ToString() ?? "Unknown";
            chartData.Labels.Add(label);
        }

        // Create dataset for each numeric column
        var colorIndex = 0;
        for (int i = 1; i < result.Columns.Count; i++)
        {
            var column = result.Columns[i];
            
            // Check if column is numeric
            if (!IsColumnNumeric(result, column))
            {
                continue;
            }

            var color = colors[colorIndex % colors.Length];
            var dataset = new ChartDataset
            {
                Label = column,
                BackgroundColor = color.Item1,
                BorderColor = color.Item2
            };

            foreach (var row in result.Rows)
            {
                var value = ConvertToDouble(row[column]);
                dataset.Data.Add(value);
            }

            chartData.Datasets.Add(dataset);
            colorIndex++;
        }

        chartData.ChartType = DetermineChartType(result);

        _logger.LogInformation("Created multi-series chart with {SeriesCount} series and {DataPoints} data points", 
            chartData.Datasets.Count, result.RowCount);
        
        return chartData;
    }

    private string DetermineChartType(QueryResult result)
    {
        // Check if first column contains date/time values - use line chart
        var firstColumn = result.Columns[0];
        var firstValue = result.Rows.FirstOrDefault()?[firstColumn]?.ToString();
        
        if (!string.IsNullOrEmpty(firstValue) && 
            (DateTime.TryParse(firstValue, out _) || 
             firstValue.Contains("Q") || 
             firstValue.Contains("Month") ||
             firstValue.Contains("Year")))
        {
            return "line";
        }

        // Default to bar chart
        return "bar";
    }

    private bool IsColumnNumeric(QueryResult result, string columnName)
    {
        // Check first few rows to determine if column is numeric
        foreach (var row in result.Rows.Take(5))
        {
            var value = row[columnName];
            if (value != null && IsNumeric(value))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsChartSuitable(QueryResult result)
    {
        // Need at least 2 columns (labels + at least one value column)
        if (result.Columns.Count < 2)
        {
            return false;
        }

        // Need at least 2 rows for meaningful visualization
        if (result.RowCount < 2)
        {
            return false;
        }

        // Don't create charts for too many data points (makes them unreadable)
        if (result.RowCount > 50)
        {
            _logger.LogInformation("Too many rows ({RowCount}) for chart - using table instead", result.RowCount);
            return false;
        }

        // Check if we have at least one numeric column (excluding first column which is labels)
        var hasNumericColumn = false;
        
        //ChartSuitable, consider these additional boundary cases for numeric column detection:

        // 1. Exclude columns that are likely to be phone numbers, IDs, or codes (e.g., "phone", "phone number", "mobile", "contact", "id", "code", "ssn", "passport", "account number").
        // 2. Exclude columns where all values are the same (no variance, not useful for charting).
        // 3. Exclude columns where values are too long (e.g., >15 digits, likely not a true metric).
        // 4. Exclude columns where >80% of values are null or empty (not enough data).
        // 5. Exclude columns where values are not positive numbers (if only positive metrics are meaningful).

        // Example: Update the exclusion regex and add checks for value length and variance.
        for (int colIndex = 1; colIndex < result.Columns.Count; colIndex++)
        {
            var column = result.Columns[colIndex];
            // Exclude columns that are likely to be identifiers or contact info
            if (Regex.IsMatch(column, @"(phone(\s*number)?|mobile|contact|id|code|ssn|passport|account(\s*number)?)", RegexOptions.IgnoreCase))
            {
                continue;
            }

            var values = result.Rows.Take(10).Select(row => row[column]).Where(v => v != null && IsNumeric(v)).ToList();
            if (values.Count == 0)
                continue;

            // Exclude if all values are the same
            if (values.Select(v => v.ToString()).Distinct().Count() == 1)
                continue;

            // Exclude if any value is too long (likely not a metric)
            if (values.Any(v => v.ToString()!.Length > 15))
                continue;

            // Exclude if too many nulls
            if (values.Count < result.Rows.Take(10).Count() * 0.2)
                continue;

            hasNumericColumn = true;
            break;
        }

        return hasNumericColumn;
    }

    private bool IsNumeric(object? value)
    {
        if (value == null) return false;

        return value is int || value is long || value is float || value is double || value is decimal ||
               double.TryParse(value.ToString(), out _);
    }

    private double ConvertToDouble(object? value)
    {
        if (value == null) return 0;

        if (value is double d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;

        if (double.TryParse(value.ToString(), out var result))
        {
            return result;
        }

        return 0;
    }
}
