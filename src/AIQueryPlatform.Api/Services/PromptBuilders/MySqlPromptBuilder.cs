using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services.PromptBuilders;

/// <summary>
/// MySQL specific prompt builder
/// </summary>
public class MySqlPromptBuilder : IDatabasePromptBuilder
{
    private readonly ILogger<MySqlPromptBuilder> _logger;

    public MySqlPromptBuilder(ILogger<MySqlPromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildSystemPrompt(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("MySQL query generator. Convert natural language to SELECT queries only.");
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
- Use LIMIT for row limiting, not TOP
- Return only SQL. No comments.
- Use CTEs (WITH clause) for complex queries when needed
- IMPORTANT: Always use table aliases (e.g., p, oi, c) and prefix ALL columns with their alias
- Example: SELECT p.ProductId, p.ProductName FROM Products p JOIN OrderItems oi ON p.ProductId = oi.ProductId GROUP BY p.ProductId, p.ProductName
- Date functions: NOW(), CURDATE(), CURTIME(), DATE_ADD(), DATE_SUB(), DATEDIFF(), YEAR(), MONTH(), DAY()
- For week calculations: WEEK() function or DATE_ADD(date, INTERVAL -1 WEEK)
- String concatenation: Use CONCAT() function
- Use IFNULL() or COALESCE() for null handling
- Use backticks for identifiers with special characters: `table_name`, `column_name`";
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

        // Convert SQL Server TOP to LIMIT
        if (Regex.IsMatch(sql, @"SELECT\s+TOP\s+(\d+)", RegexOptions.IgnoreCase))
        {
            var match = Regex.Match(sql, @"SELECT\s+TOP\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var limit = match.Groups[1].Value;
                sql = Regex.Replace(sql, @"SELECT\s+TOP\s+\d+\s+", "SELECT ", RegexOptions.IgnoreCase);
                sql = sql.TrimEnd(';') + $" LIMIT {limit}";
                _logger.LogInformation("Converted TOP to LIMIT for MySQL");
            }
        }

        // Convert SQL Server date functions to MySQL equivalents
        sql = Regex.Replace(sql, @"\bGETDATE\(\)", "NOW()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bGETUTCDATE\(\)", "UTC_TIMESTAMP()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bISNULL\(", "IFNULL(", RegexOptions.IgnoreCase);

        // Remove trailing semicolons (we can add them back consistently)
        sql = sql.TrimEnd(';');

        return sql;
    }
}
