using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ScottPlot;
using System.Text;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for generating PDF reports
/// </summary>
public class ReportingService : IReportingService
{
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(ILogger<ReportingService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfReportAsync(
        QueryResult result,
        string reportTitle,
        Tenant tenant,
        ChartData? chartData = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF report: {Title}", reportTitle);

            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Add header with branding
            AddHeader(document, reportTitle, tenant);

            // Add report metadata
            AddMetadata(document);

            // Add table data
            AddTableData(document, result);

            // Add chart info if available
            if (chartData != null)
            {
                AddChartInfo(document, chartData);
            }

            // Add footer
            AddFooter(document, tenant);

            document.Close();

            _logger.LogInformation("PDF report generated successfully");

            return await Task.FromResult(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report");
            throw new InvalidOperationException("Failed to generate PDF report", ex);
        }
    }

    private void AddHeader(Document document, string title, Tenant tenant)
    {
        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, tenant.ThemeColor != null ? 
            new BaseColor(System.Drawing.ColorTranslator.FromHtml(tenant.ThemeColor)) : new BaseColor(0, 0, 255));
        var titleParagraph = new Paragraph(title, titleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 10
        };
        document.Add(titleParagraph);

        // Tenant name
        var tenantFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, new BaseColor(128, 128, 128));
        var tenantParagraph = new Paragraph(tenant.Name, tenantFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(tenantParagraph);

        // Separator line
        var line = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator()))
        {
            SpacingAfter = 15
        };
        document.Add(line);
    }

    private void AddMetadata(Document document)
    {
        var metadataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(64, 64, 64));
        var metadata = new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC", metadataFont)
        {
            Alignment = Element.ALIGN_RIGHT,
            SpacingAfter = 15
        };
        document.Add(metadata);
    }

    private void AddTableData(Document document, QueryResult result)
    {
        if (result.RowCount == 0)
        {
            var noDataFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 12, new BaseColor(128, 128, 128));
            document.Add(new Paragraph("No data available", noDataFont));
            return;
        }

        // Create table
        var table = new PdfPTable(result.Columns.Count)
        {
            WidthPercentage = 100,
            SpacingBefore = 10,
            SpacingAfter = 15
        };

        // Header row
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(255, 255, 255));
        foreach (var column in result.Columns)
        {
            var cell = new PdfPCell(new Phrase(column, headerFont))
            {
                BackgroundColor = new BaseColor(64, 64, 64),
                Padding = 5,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        // Data rows
        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(0, 0, 0));
        var alternateColor = new BaseColor(240, 240, 240);

        for (int i = 0; i < result.Rows.Count; i++)
        {
            var row = result.Rows[i];
            var isAlternate = i % 2 == 1;

            foreach (var column in result.Columns)
            {
                var value = row[column]?.ToString() ?? "";
                var cell = new PdfPCell(new Phrase(value, dataFont))
                {
                    Padding = 5,
                    BackgroundColor = isAlternate ? alternateColor : new BaseColor(255, 255, 255)
                };
                table.AddCell(cell);
            }
        }

        document.Add(table);

        // Row count
        var countFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(128, 128, 128));
        var countParagraph = new Paragraph($"Total rows: {result.RowCount}", countFont)
        {
            SpacingBefore = 10
        };
        document.Add(countParagraph);
    }

    private void AddChartInfo(Document document, ChartData chartData)
    {
        var chartFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(64, 64, 64));
        var chartTitle = new Paragraph("Visual Data Summary", chartFont)
        {
            SpacingBefore = 20,
            SpacingAfter = 15
        };
        document.Add(chartTitle);

        // Ensure we have valid data
        if (chartData == null || chartData.Datasets == null || chartData.Datasets.Count == 0)
        {
            _logger.LogWarning("Chart data is null or empty - Labels: {LabelCount}, Datasets: {DatasetCount}", 
                chartData?.Labels?.Count ?? 0, chartData?.Datasets?.Count ?? 0);
            var noDataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(128, 128, 128));
            document.Add(new Paragraph("No chart data available", noDataFont) { SpacingAfter = 10 });
            return;
        }

        _logger.LogInformation("Generating chart visualization - Labels: {LabelCount}, Datasets: {DatasetCount}", 
            chartData.Labels?.Count ?? 0, chartData.Datasets.Count);

        try
        {
            // Generate chart image using ScottPlot
            var chartImageBytes = GenerateChartImage(chartData);
            
            if (chartImageBytes != null && chartImageBytes.Length > 0)
            {
                // Embed chart image in PDF
                var chartImage = iTextSharp.text.Image.GetInstance(chartImageBytes);
                
                // Scale image to fit page width (with margins)
                var maxWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                var maxHeight = 300f; // Maximum height for chart
                
                if (chartImage.Width > maxWidth)
                {
                    var ratio = maxWidth / chartImage.Width;
                    chartImage.ScalePercent(ratio * 100);
                }
                
                if (chartImage.ScaledHeight > maxHeight)
                {
                    var ratio = maxHeight / chartImage.ScaledHeight;
                    chartImage.ScalePercent(chartImage.ScaledWidth / chartImage.Width * ratio * 100);
                }
                
                chartImage.Alignment = Element.ALIGN_CENTER;
                chartImage.SpacingAfter = 20;
                
                document.Add(chartImage);
                
                _logger.LogInformation("Chart image embedded in PDF successfully");
            }
            else
            {
                _logger.LogWarning("Chart image generation returned empty result");
                AddChartTableFallback(document, chartData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart image, falling back to table representation");
            AddChartTableFallback(document, chartData);
        }
    }

    private byte[]? GenerateChartImage(ChartData chartData)
    {
        try
        {
            // Create plot
            var plt = new Plot(800, 400);
            
            // Configure plot styling
            plt.Style(ScottPlot.Style.Light1);
            plt.YLabel("Values");
            plt.XLabel("Categories");
            
            // Prepare data
            var labels = chartData.Labels?.ToArray() ?? Array.Empty<string>();
            var positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
            
            // Define color palette matching frontend
            var colors = new[]
            {
                System.Drawing.Color.FromArgb(54, 162, 235),   // Blue
                System.Drawing.Color.FromArgb(255, 99, 132),   // Red
                System.Drawing.Color.FromArgb(75, 192, 192),   // Teal
                System.Drawing.Color.FromArgb(255, 206, 86),   // Yellow
                System.Drawing.Color.FromArgb(153, 102, 255),  // Purple
                System.Drawing.Color.FromArgb(255, 159, 64)    // Orange
            };
            
            if (chartData.ChartType?.ToLower() == "line")
            {
                // Line chart
                for (int i = 0; i < chartData.Datasets.Count; i++)
                {
                    var dataset = chartData.Datasets[i];
                    var values = dataset.Data?.ToArray() ?? Array.Empty<double>();
                    
                    if (values.Length > 0)
                    {
                        var color = colors[i % colors.Length];
                        var signalPlot = plt.AddSignal(values, label: dataset.Label);
                        signalPlot.Color = color;
                        signalPlot.LineWidth = 2;
                    }
                }
            }
            else
            {
                // Bar chart (default)
                var barWidth = 0.8 / chartData.Datasets.Count;
                var offset = -(chartData.Datasets.Count - 1) * barWidth / 2;
                
                for (int i = 0; i < chartData.Datasets.Count; i++)
                {
                    var dataset = chartData.Datasets[i];
                    var values = dataset.Data?.ToArray() ?? Array.Empty<double>();
                    
                    if (values.Length > 0)
                    {
                        var barPositions = positions.Select(p => p + offset + (i * barWidth)).ToArray();
                        var color = colors[i % colors.Length];
                        
                        var barPlot = plt.AddBar(values, barPositions);
                        barPlot.BarWidth = barWidth;
                        barPlot.FillColor = color;
                        barPlot.Label = dataset.Label;
                    }
                }
            }
            
            // Set X-axis labels
            if (labels.Length > 0)
            {
                plt.XTicks(positions, labels);
            }
            
            // Add legend if multiple datasets
            if (chartData.Datasets.Count > 1)
            {
                plt.Legend(location: Alignment.UpperRight);
            }
            
            // Configure axis limits
            plt.SetAxisLimits(yMin: 0);
            
            // Generate PNG image
            var imageBytes = plt.GetImageBytes();
            
            _logger.LogInformation("Generated chart image: {ByteCount} bytes", imageBytes?.Length ?? 0);
            
            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart image with ScottPlot");
            return null;
        }
    }

    private void AddChartTableFallback(Document document, ChartData chartData)
    {
        // Fallback: text-based table representation
        var dataPointCount = chartData.Datasets.Max(d => d.Data?.Count ?? 0);
        
        // If Labels is empty but we have data, generate labels
        if ((chartData.Labels == null || chartData.Labels.Count == 0) && dataPointCount > 0)
        {
            chartData.Labels = new List<string>();
            for (int i = 0; i < dataPointCount; i++)
            {
                chartData.Labels.Add($"Row {i + 1}");
            }
        }

        // Create a visual table representation of the chart data
        var columnCount = 1 + chartData.Datasets.Count; // Labels + each dataset
        var chartTable = new PdfPTable(columnCount)
        {
            WidthPercentage = 100,
            SpacingBefore = 10,
            SpacingAfter = 15
        };

        // Header row
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(255, 255, 255));
        
        var labelHeaderCell = new PdfPCell(new Phrase("Period/Category", headerFont))
        {
            BackgroundColor = new BaseColor(64, 64, 64),
            Padding = 8,
            HorizontalAlignment = Element.ALIGN_CENTER
        };
        chartTable.AddCell(labelHeaderCell);

        foreach (var dataset in chartData.Datasets)
        {
            var dataHeaderCell = new PdfPCell(new Phrase(dataset.Label, headerFont))
            {
                BackgroundColor = new BaseColor(64, 64, 64),
                Padding = 8,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            chartTable.AddCell(dataHeaderCell);
        }

        // Data rows
        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(0, 0, 0));
        var rowCount = Math.Max(chartData.Labels?.Count ?? 0, dataPointCount);
        
        for (int i = 0; i < rowCount; i++)
        {
            var label = i < (chartData.Labels?.Count ?? 0) ? chartData.Labels[i] : $"Row {i + 1}";
            var labelCell = new PdfPCell(new Phrase(label, dataFont))
            {
                Padding = 8,
                BackgroundColor = i % 2 == 0 ? new BaseColor(255, 255, 255) : new BaseColor(245, 245, 245)
            };
            chartTable.AddCell(labelCell);

            foreach (var dataset in chartData.Datasets)
            {
                if (dataset.Data != null && i < dataset.Data.Count)
                {
                    var value = dataset.Data[i];
                    var valueCell = new PdfPCell(new Phrase(value.ToString("N2"), dataFont))
                    {
                        Padding = 8,
                        BackgroundColor = i % 2 == 0 ? new BaseColor(255, 255, 255) : new BaseColor(245, 245, 245),
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    };
                    chartTable.AddCell(valueCell);
                }
                else
                {
                    chartTable.AddCell(new PdfPCell(new Phrase("-", dataFont))
                    {
                        Padding = 8,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = i % 2 == 0 ? new BaseColor(255, 255, 255) : new BaseColor(245, 245, 245)
                    });
                }
            }
        }

        document.Add(chartTable);

        // Add summary statistics
        if (chartData.Datasets.Count > 0)
        {
            var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(100, 100, 100));
            var summaryText = new Paragraph()
            {
                SpacingBefore = 10
            };

            foreach (var dataset in chartData.Datasets)
            {
                if (dataset.Data != null && dataset.Data.Any())
                {
                    var avg = dataset.Data.Average();
                    var min = dataset.Data.Min();
                    var max = dataset.Data.Max();
                    var total = dataset.Data.Sum();

                    summaryText.Add(new Chunk($"{dataset.Label}: ", 
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(64, 64, 64))));
                    summaryText.Add(new Chunk($"Total={total:N2}, Avg={avg:N2}, Min={min:N2}, Max={max:N2}\n", summaryFont));
                }
            }

            if (summaryText.Count > 0)
            {
                document.Add(summaryText);
            }
        }

        // Chart type info
        var infoFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, new BaseColor(128, 128, 128));
        var chartTypeInfo = new Paragraph($"Chart Type: {chartData.ChartType} | Data Points: {chartData.Labels.Count}", infoFont)
        {
            SpacingBefore = 10,
            Alignment = Element.ALIGN_CENTER
        };
        document.Add(chartTypeInfo);
    }

    private void AddFooter(Document document, Tenant tenant)
    {
        var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, new BaseColor(128, 128, 128));
        var footer = new Paragraph($"© {DateTime.UtcNow.Year} {tenant.Name}. All rights reserved.", footerFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingBefore = 20
        };
        document.Add(footer);
    }
}
