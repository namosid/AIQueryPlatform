using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services.PromptBuilders;

/// <summary>
/// Excel (OleDb) specific prompt builder
/// </summary>
public class ExcelPromptBuilder : IDatabasePromptBuilder
{
    private readonly ILogger<ExcelPromptBuilder> _logger;

    public ExcelPromptBuilder(ILogger<ExcelPromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildSystemPrompt(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Excel OleDb query generator. Convert natural language to SELECT queries for Excel sheets.");
        sb.AppendLine(GetSyntaxRules());
        sb.AppendLine();
        sb.AppendLine("Available Sheets (use as table names with $ suffix):");

        foreach (var table in schema.Tables)
        {
            // Excel sheets are referenced as [SheetName$]
            var sheetName = table.TableName.EndsWith("$") ? table.TableName : table.TableName + "$";
            sb.Append($"[{sheetName}](");
            var columns = new List<string>();
            foreach (var column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            sb.Append(string.Join(",", columns));
            sb.AppendLine(")");
        }

        return sb.ToString();
    }

    public string GetSyntaxRules()
    {
        return @"Rules for Excel OleDb: 
- Reference sheets as [SheetName$] with square brackets and $ suffix
- Use TOP N for limiting rows, not LIMIT
- Return only SQL. No comments.
- JOINs are supported but complex queries may be slow
- Column names with spaces must be in brackets: [Column Name]
- Date functions are limited - use basic comparison operators
- String functions: LEN(), LEFT(), RIGHT(), MID(), TRIM()
- Aggregates: SUM(), AVG(), COUNT(), MIN(), MAX()
- ORDER BY and GROUP BY are supported
- WHERE clause for filtering
- No CTEs or window functions
- Example: SELECT TOP 10 [ProductName], [Price] FROM [Products$] WHERE [Price] > 100 ORDER BY [Price] DESC";
    }

    public string CleanSqlResponse(string sql)
    {
        // Remove markdown code blocks
        sql = sql.Replace("```sql", "").Replace("```", "").Trim();
        
        // Remove common prefixes
        if (sql.StartsWith("SQL:", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql.Substring(4).Trim();
        }

        // Ensure sheet names have $ suffix and are bracketed
        sql = Regex.Replace(sql, @"FROM\s+(\w+)(?!\$)", "FROM [$1$]", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"JOIN\s+(\w+)(?!\$)", "JOIN [$1$]", RegexOptions.IgnoreCase);

        // Convert LIMIT to TOP
        if (Regex.IsMatch(sql, @"LIMIT\s+\d+", RegexOptions.IgnoreCase))
        {
            var match = Regex.Match(sql, @"LIMIT\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var limit = match.Groups[1].Value;
                sql = Regex.Replace(sql, @"\s*LIMIT\s+\d+\s*$", "", RegexOptions.IgnoreCase);
                if (!Regex.IsMatch(sql, @"SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
                {
                    sql = Regex.Replace(sql, @"^SELECT\s+", $"SELECT TOP {limit} ", RegexOptions.IgnoreCase);
                }
                _logger.LogInformation("Converted LIMIT to TOP for Excel");
            }
        }

        // Remove unsupported features
        if (sql.Contains("WITH ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("CTEs are not supported in Excel OleDb queries");
        }

        // Remove trailing semicolons
        sql = sql.TrimEnd(';');

        return sql;
    }
}
