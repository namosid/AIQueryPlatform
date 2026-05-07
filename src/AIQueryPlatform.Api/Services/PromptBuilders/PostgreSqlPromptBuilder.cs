using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services.PromptBuilders;

/// <summary>
/// PostgreSQL specific prompt builder
/// </summary>
public class PostgreSqlPromptBuilder : IDatabasePromptBuilder
{
    private readonly ILogger<PostgreSqlPromptBuilder> _logger;

    public PostgreSqlPromptBuilder(ILogger<PostgreSqlPromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildSystemPrompt(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("PostgreSQL query generator. Convert natural language to SELECT queries only.");
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
- Use LIMIT and OFFSET for pagination, not TOP
- Return only SQL. No comments.
- Use CTEs (WITH clause) for complex queries
- IMPORTANT: Always use table aliases (e.g., p, oi, c) and prefix ALL columns with their alias
- Example: SELECT p.ProductId, p.ProductName FROM Products p JOIN OrderItems oi ON p.ProductId = oi.ProductId GROUP BY p.ProductId, p.ProductName
- Date functions: NOW(), CURRENT_DATE, CURRENT_TIMESTAMP, date_trunc(), INTERVAL, AGE()
- For week calculations: date_trunc('week', date) or date + INTERVAL '-1 week'
- String concatenation: Use || operator or CONCAT() function
- Use COALESCE() for null handling
- PostgreSQL is case-sensitive for identifiers - use double quotes for exact case: ""TableName""
- Use single quotes for string literals";
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
                _logger.LogInformation("Converted TOP to LIMIT for PostgreSQL");
            }
        }

        // Convert SQL Server date functions to PostgreSQL equivalents
        sql = Regex.Replace(sql, @"\bGETDATE\(\)", "NOW()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bGETUTCDATE\(\)", "NOW() AT TIME ZONE 'UTC'", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bISNULL\(", "COALESCE(", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bDATEADD\(", "date_add(", RegexOptions.IgnoreCase);

        // Remove trailing semicolons
        sql = sql.TrimEnd(';');

        return sql;
    }
}
