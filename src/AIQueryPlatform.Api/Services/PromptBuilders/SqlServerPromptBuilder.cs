using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services.PromptBuilders;

/// <summary>
/// SQL Server specific prompt builder
/// </summary>
public class SqlServerPromptBuilder : IDatabasePromptBuilder
{
    private readonly ILogger<SqlServerPromptBuilder> _logger;

    public SqlServerPromptBuilder(ILogger<SqlServerPromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildSystemPrompt(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("SQL Server query generator. Convert natural language to SELECT queries only.");
        sb.AppendLine(GetSyntaxRules());
        sb.AppendLine();
        sb.AppendLine("Schema:");

        foreach (var table in schema.Tables)
        {
            sb.Append($"{table.TableName}(");
            var columns = new List<string>();
            foreach (var column in table.Columns)
            {
                var col = column.ColumnName;
                if (column.IsPrimaryKey) col += "*";
                columns.Add(col);
            }
            sb.Append(string.Join(",", columns));
            sb.AppendLine(")");
        }

        return sb.ToString();
    }

    public string GetSyntaxRules()
    {
        return @"Rules: 
- Use TOP not LIMIT or OFFSET/FETCH. Never use TOP and OFFSET together. 
- Return only SQL. No comments. 
- Use CTEs for complex queries. Never nest aggregates.
- For top N queries: SELECT TOP N ... FROM ... ORDER BY ... (no OFFSET)
- IMPORTANT: Always use table aliases (e.g., p, oi, c) and prefix ALL columns with their alias: p.ProductId, oi.Quantity. Never use bare column names when joining tables.
- Example: SELECT p.ProductId, p.ProductName FROM Products p JOIN OrderItems oi ON p.ProductId = oi.ProductId GROUP BY p.ProductId, p.ProductName
- Date functions: GETDATE(), DATEADD(week,-1,GETDATE()), DATEDIFF(week,date1,date2), YEAR(date), MONTH(date)
- Week-over-week: Use LAG() OVER (ORDER BY week) or self-join with DATEADD(week,-1,...)
- String concatenation: Use + operator or CONCAT()
- ISNULL() for null handling";
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

        // Convert LIMIT to TOP for SQL Server compatibility
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
                _logger.LogInformation("Converted LIMIT to TOP in AI response for SQL Server");
            }
        }

        // Remove OFFSET/FETCH NEXT if TOP is present
        if (Regex.IsMatch(sql, @"SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
        {
            if (Regex.IsMatch(sql, @"OFFSET\s+\d+\s+ROWS?", RegexOptions.IgnoreCase))
            {
                sql = Regex.Replace(sql, @"\s*OFFSET\s+\d+\s+ROWS?\s*(FETCH\s+NEXT\s+\d+\s+ROWS?\s+ONLY)?", "", RegexOptions.IgnoreCase);
                _logger.LogInformation("Removed OFFSET clause because TOP is already present");
            }
        }

        // Convert MySQL/PostgreSQL date functions to SQL Server equivalents
        sql = Regex.Replace(sql, @"\bCURRENT_DATE\b", "CAST(GETDATE() AS DATE)", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bCURRENT_TIMESTAMP\b", "GETDATE()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bNOW\(\)", "GETDATE()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bCURDATE\(\)", "CAST(GETDATE() AS DATE)", RegexOptions.IgnoreCase);

        // Remove trailing semicolons
        sql = sql.TrimEnd(';');

        return sql;
    }
}
