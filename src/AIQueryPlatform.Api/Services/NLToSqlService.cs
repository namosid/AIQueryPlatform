using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using System.Text;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for converting natural language to SQL using OpenAI
/// </summary>
public class NLToSqlService : INLToSqlService
{
    private readonly ILogger<NLToSqlService> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;
    private string? _cachedSystemPromptBase;
    private DatabaseSchema? _cachedSchema;

    public NLToSqlService(
        IConfiguration configuration,
        ILogger<NLToSqlService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["OpenAI:Endpoint"] ?? throw new ArgumentNullException("OpenAI:Endpoint");
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
        _deploymentName = configuration["OpenAI:DeploymentName"] ?? "gpt-4";
        _maxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 500);
        _temperature = configuration.GetValue<float>("OpenAI:Temperature", 0.0f);

        _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<string> ConvertNaturalLanguageToSqlAsync(string query, DatabaseSchema schema)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(schema);
            var userPrompt = query;

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = _maxTokens,
                Temperature = _temperature
            };

            _logger.LogInformation("Sending request to OpenAI for query: {Query}", query);

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var sqlQuery = response.Value.Choices[0].Message.Content;

            // Clean up the response
            sqlQuery = CleanSqlResponse(sqlQuery);

            // Validate SQL syntax
            if (!ValidateSqlSyntax(sqlQuery, out var syntaxError))
            {
                _logger.LogWarning("Generated SQL has syntax issues: {Error}. Attempting to fix...", syntaxError);
                // For now, log the error but still return the query
                // In production, you might want to retry with a corrected prompt
            }

            _logger.LogInformation("Generated SQL: {Sql}", sqlQuery);

            return sqlQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting natural language to SQL");
            throw new InvalidOperationException("Failed to generate SQL query", ex);
        }
    }

    private string BuildSystemPrompt(DatabaseSchema schema)
    {
        // Cache system prompt base if schema hasn't changed
        if (_cachedSchema != null && _cachedSystemPromptBase != null && 
            SchemaMatches(_cachedSchema, schema))
        {
            return _cachedSystemPromptBase;
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("SQL Server query generator. Convert natural language to SELECT queries only.");
        sb.AppendLine("Rules: Use TOP not LIMIT or OFFSET/FETCH. Never use TOP and OFFSET together. Return only SQL. No comments. Use CTEs for complex queries. Never nest aggregates.");
        sb.AppendLine("For top N queries: SELECT TOP N ... FROM ... ORDER BY ... (no OFFSET)");
        sb.AppendLine("IMPORTANT: Always use table aliases (e.g., p, oi, c) and prefix ALL columns with their alias: p.ProductId, oi.Quantity. Never use bare column names when joining tables.");
        sb.AppendLine("Example: SELECT p.ProductId, p.ProductName FROM Products p JOIN OrderItems oi ON p.ProductId = oi.ProductId GROUP BY p.ProductId, p.ProductName");
        sb.AppendLine("Date functions: GETDATE(), DATEADD(week,-1,GETDATE()), DATEDIFF(week,date1,date2), YEAR(date), MONTH(date)");
        sb.AppendLine("Week-over-week: Use LAG() OVER (ORDER BY week) or self-join with DATEADD(week,-1,...)");
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

        var prompt = sb.ToString();
        _cachedSystemPromptBase = prompt;
        _cachedSchema = schema;
        return prompt;
    }

    private bool SchemaMatches(DatabaseSchema schema1, DatabaseSchema schema2)
    {
        if (schema1.Tables.Count != schema2.Tables.Count) return false;
        for (int i = 0; i < schema1.Tables.Count; i++)
        {
            if (schema1.Tables[i].TableName != schema2.Tables[i].TableName) return false;
            if (schema1.Tables[i].Columns.Count != schema2.Tables[i].Columns.Count) return false;
        }
        return true;
    }

    private string CleanSqlResponse(string sql)
    {
        // Remove markdown code blocks
        sql = sql.Replace("```sql", "").Replace("```", "").Trim();
        
        // Remove common prefixes
        if (sql.StartsWith("SQL:", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql.Substring(4).Trim();
        }

        // Convert LIMIT to TOP for SQL Server compatibility (in case AI still uses it)
        if (Regex.IsMatch(sql, @"LIMIT\s+\d+", RegexOptions.IgnoreCase))
        {
            var match = Regex.Match(sql, @"LIMIT\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var limit = match.Groups[1].Value;
                // Remove LIMIT clause
                sql = Regex.Replace(sql, @"\s*LIMIT\s+\d+\s*$", "", RegexOptions.IgnoreCase);
                // Add TOP if not already present
                if (!Regex.IsMatch(sql, @"SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
                {
                    sql = Regex.Replace(sql, @"^SELECT\s+", $"SELECT TOP {limit} ", RegexOptions.IgnoreCase);
                }
                _logger.LogInformation("Converted LIMIT to TOP in AI response for SQL Server");
            }
        }

        // Remove OFFSET/FETCH NEXT if TOP is present (SQL Server doesn't allow both)
        if (Regex.IsMatch(sql, @"SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
        {
            // Remove OFFSET clause
            if (Regex.IsMatch(sql, @"OFFSET\s+\d+\s+ROWS?", RegexOptions.IgnoreCase))
            {
                sql = Regex.Replace(sql, @"\s*OFFSET\s+\d+\s+ROWS?\s*(FETCH\s+NEXT\s+\d+\s+ROWS?\s+ONLY)?", "", RegexOptions.IgnoreCase);
                _logger.LogInformation("Removed OFFSET clause because TOP is already present");
            }
        }
        // If OFFSET is used without TOP, convert to TOP (for simple cases)
        else if (Regex.IsMatch(sql, @"OFFSET\s+0\s+ROWS\s+FETCH\s+NEXT\s+(\d+)\s+ROWS?\s+ONLY", RegexOptions.IgnoreCase))
        {
            var match = Regex.Match(sql, @"FETCH\s+NEXT\s+(\d+)\s+ROWS?\s+ONLY", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var fetchCount = match.Groups[1].Value;
                // Remove OFFSET/FETCH
                sql = Regex.Replace(sql, @"\s*OFFSET\s+\d+\s+ROWS?\s+FETCH\s+NEXT\s+\d+\s+ROWS?\s+ONLY", "", RegexOptions.IgnoreCase);
                // Add TOP
                sql = Regex.Replace(sql, @"^SELECT\s+", $"SELECT TOP {fetchCount} ", RegexOptions.IgnoreCase);
                _logger.LogInformation("Converted OFFSET/FETCH NEXT to TOP for SQL Server compatibility");
            }
        }

        // Convert MySQL/PostgreSQL date functions to SQL Server equivalents
        sql = Regex.Replace(sql, @"\bCURRENT_DATE\b", "CAST(GETDATE() AS DATE)", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bCURRENT_TIMESTAMP\b", "GETDATE()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bNOW\(\)", "GETDATE()", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bCURDATE\(\)", "CAST(GETDATE() AS DATE)", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bCURTIME\(\)", "CAST(GETDATE() AS TIME)", RegexOptions.IgnoreCase);

        // Remove trailing semicolons
        sql = sql.TrimEnd(';');

        return sql;
    }

    private bool ValidateSqlSyntax(string sql, out string? errorMessage)
    {
        errorMessage = null;

        // Check for balanced parentheses
        var openParens = 0;
        var lineNumber = 1;
        var charPosition = 0;

        foreach (var c in sql)
        {
            charPosition++;
            if (c == '\n')
            {
                lineNumber++;
                charPosition = 0;
            }

            if (c == '(') openParens++;
            if (c == ')')
            {
                openParens--;
                if (openParens < 0)
                {
                    errorMessage = $"Unbalanced parentheses: extra closing parenthesis at line {lineNumber}, position {charPosition}";
                    return false;
                }
            }
        }

        if (openParens != 0)
        {
            errorMessage = $"Unbalanced parentheses: {Math.Abs(openParens)} unclosed opening parenthesis(es)";
            return false;
        }

        // Check for basic SQL Server syntax requirements
        if (!sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !sql.Trim().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Query must start with SELECT or WITH (for CTEs)";
            return false;
        }

        return true;
    }
}
