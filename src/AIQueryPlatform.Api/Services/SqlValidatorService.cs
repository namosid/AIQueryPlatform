using AIQueryPlatform.Api.Services.Interfaces;
using System.Text.RegularExpressions;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for validating SQL queries and ensuring security
/// </summary>
public class SqlValidatorService : ISqlValidatorService
{
    private readonly ILogger<SqlValidatorService> _logger;
    
    // Dangerous keywords that should be blocked
    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXECUTE", "EXEC", "MERGE", "GRANT", "REVOKE", "DENY",
        "xp_", "sp_executesql", "OPENROWSET", "OPENDATASOURCE"
    };

    public SqlValidatorService(ILogger<SqlValidatorService> logger)
    {
        _logger = logger;
    }

    public bool IsValidSelectQuery(string sql, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(sql))
        {
            errorMessage = "SQL query cannot be empty";
            return false;
        }

        // Remove comments and extra whitespace
        sql = RemoveComments(sql).Trim();

        // Must start with SELECT or WITH (for CTEs)
        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Only SELECT queries (including CTEs starting with WITH) are allowed";
            _logger.LogWarning("Blocked non-SELECT query: {Sql}", sql);
            return false;
        }

        // If it's a CTE, verify it contains SELECT
        if (sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            if (!sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "CTE (WITH clause) must contain a SELECT statement";
                _logger.LogWarning("Blocked CTE without SELECT: {Sql}", sql);
                return false;
            }
        }

        // Check for dangerous keywords
        foreach (var keyword in DangerousKeywords)
        {
            if (Regex.IsMatch(sql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                errorMessage = $"Dangerous SQL keyword detected: {keyword}";
                _logger.LogWarning("Blocked query with dangerous keyword {Keyword}: {Sql}", keyword, sql);
                return false;
            }
        }

        // Check for common SQL injection patterns
        if (ContainsSqlInjectionPattern(sql))
        {
            errorMessage = "Potential SQL injection pattern detected";
            _logger.LogWarning("Blocked query with SQL injection pattern: {Sql}", sql);
            return false;
        }

        // Check for multiple statements (semicolons not at the end)
        var semicolonCount = sql.Count(c => c == ';');
        if (semicolonCount > 1 || (semicolonCount == 1 && !sql.TrimEnd().EndsWith(";")))
        {
            errorMessage = "Multiple SQL statements are not allowed";
            _logger.LogWarning("Blocked query with multiple statements: {Sql}", sql);
            return false;
        }

        return true;
    }

    public string EnforceRowLimit(string sql, int maxRows)
    {
        sql = sql.TrimEnd(';').Trim();

        // Check if query already has TOP clause (SQL Server)
        if (Regex.IsMatch(sql, @"SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
        {
            // Replace existing TOP with our max
            sql = Regex.Replace(
                sql,
                @"(SELECT\s+)TOP\s+(\d+)",
                m =>
                {
                    int currentTop = int.TryParse(m.Groups[2].Value, out var n) ? n : 0;
                    if (currentTop > maxRows)
                    {
                        return $"{m.Groups[1].Value}TOP {maxRows}";
                    }
                    return m.Value;
                },
                RegexOptions.IgnoreCase
            );
        }
        // Check if query has LIMIT clause (MySQL/PostgreSQL) - convert to TOP for SQL Server
        else if (Regex.IsMatch(sql, @"LIMIT\s+\d+", RegexOptions.IgnoreCase))
        {
            // Remove LIMIT clause
            sql = Regex.Replace(sql, @"\s*LIMIT\s+\d+\s*$", "", RegexOptions.IgnoreCase);
            
            // Handle CTEs - add TOP to the final SELECT
            if (sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                sql = AddTopToFinalSelect(sql, maxRows);
            }
            else
            {
                // Add TOP clause to simple SELECT
                sql = Regex.Replace(sql, @"^SELECT\s+", $"SELECT TOP {maxRows} ", RegexOptions.IgnoreCase);
            }
            _logger.LogInformation("Converted LIMIT to TOP for SQL Server compatibility");
        }
        else
        {
            // Handle CTEs - add TOP to the final SELECT
            if (sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                sql = AddTopToFinalSelect(sql, maxRows);
            }
            else
            {
                // Add TOP clause for simple SELECT
                sql = Regex.Replace(sql, @"^SELECT\s+", $"SELECT TOP {maxRows} ", RegexOptions.IgnoreCase);
            }
        }

        return sql;
    }

    private string AddTopToFinalSelect(string sql, int maxRows)
    {
        // For CTEs, we need to find the final SELECT statement (after the CTE definitions)
        // Pattern: WITH ... ) SELECT ... or WITH ... ), SELECT ...
        
        // Find the last occurrence of SELECT that's outside the CTE definitions
        // This is a simplified approach - finds the last SELECT statement
        var lastSelectIndex = sql.LastIndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        
        if (lastSelectIndex > 0)
        {
            // Check if this SELECT already has TOP
            var selectPart = sql.Substring(lastSelectIndex);
            if (!Regex.IsMatch(selectPart, @"^SELECT\s+TOP\s+\d+", RegexOptions.IgnoreCase))
            {
                // Insert TOP after SELECT
                var beforeSelect = sql.Substring(0, lastSelectIndex);
                var afterSelect = sql.Substring(lastSelectIndex);
                afterSelect = Regex.Replace(afterSelect, @"^SELECT\s+", $"SELECT TOP {maxRows} ", RegexOptions.IgnoreCase);
                sql = beforeSelect + afterSelect;
                _logger.LogInformation("Added TOP {MaxRows} to final SELECT in CTE", maxRows);
            }
        }
        
        return sql;
    }

    private string RemoveComments(string sql)
    {
        // Remove single-line comments (-- ...)
        sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
        
        // Remove multi-line comments (/* ... */)
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        return sql;
    }

    private bool ContainsSqlInjectionPattern(string sql)
    {
        // Check for common SQL injection patterns
        var injectionPatterns = new[]
        {
            @"'.*--",                           // Comment after quote
            @"';.*--",                          // Semicolon, command, comment
            @"\bOR\b\s+['""]*\d+\s*=\s*\d+",   // OR 1=1, OR '1'='1' patterns
            @"\bAND\b\s+['""]*\d+\s*=\s*\d+",  // AND 1=1, AND '1'='1' patterns
            @"\bOR\b\s+['""]*[a-z]+['""]*\s*=\s*['""]*[a-z]+['""]*", // OR 'x'='x' patterns
            @"UNION\s+SELECT",                  // UNION SELECT attacks
            @"xp_cmdshell",                     // Command shell
            @"INTO\s+OUTFILE",                  // File operations
            @"LOAD_FILE",                       // File operations
            @"1\s*=\s*1",                       // Direct 1=1 injection
            @"''\s*=\s*''",                     // Direct ''='' injection
        };

        foreach (var pattern in injectionPatterns)
        {
            if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
