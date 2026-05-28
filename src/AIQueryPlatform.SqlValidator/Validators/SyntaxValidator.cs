using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using AIQueryPlatform.SqlValidator.Models;

namespace AIQueryPlatform.SqlValidator.Validators;

/// <summary>
/// Layer 1 — Syntax Validation (no DB needed).
/// Checks for basic SQL structure issues using regex + token analysis.
/// For production, swap inner checks with a proper parser like
/// Microsoft.SqlServer.TransactSql.ScriptDom (available on NuGet).
/// </summary>
public class SyntaxValidator
{
    // Required clauses for a valid SELECT statement
    private static readonly string[] RequiredClauses = ["SELECT", "FROM"];

    // Unmatched bracket/paren patterns
    private static readonly Regex SingleQuoteRegex =
        new(@"'(?:[^']|'')*'", RegexOptions.Compiled);

    private static readonly Regex CommentRegex =
        new(@"--[^\n]*|/\*[\s\S]*?\*/", RegexOptions.Compiled);

    public ValidationResult Validate(string sql)
    {
        var result = new ValidationResult
        {
            SuggestedNextState = TurnState.SQLError
        };

        if (string.IsNullOrWhiteSpace(sql))
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Syntax,
                Severity = IssueSeverity.Error,
                Code = "SYN001",
                Message = "SQL query is empty.",
                Suggestion = "Ensure the LLM generated a SQL statement."
            });
            return result;
        }

        // Strip comments and string literals for structural analysis
        var stripped = CommentRegex.Replace(sql, " ");
        stripped = SingleQuoteRegex.Replace(stripped, "''");
        var upper = stripped.ToUpperInvariant();

        // ── Check 1: Required clauses ──────────────────────────────────────
        foreach (var clause in RequiredClauses)
        {
            if (!upper.Contains(clause))
            {
                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.Syntax,
                    Severity = IssueSeverity.Error,
                    Code = "SYN002",
                    Message = $"Missing required clause: {clause}.",
                    Suggestion = $"Add a {clause} clause to the query."
                });
            }
        }

        // ── Check 2: Balanced parentheses ─────────────────────────────────
        var depth = 0;
        foreach (var ch in stripped)
        {
            if (ch == '(') depth++;
            else if (ch == ')') depth--;

            if (depth < 0) break;
        }
        if (depth != 0)
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Syntax,
                Severity = IssueSeverity.Error,
                Code = "SYN003",
                Message = "Unbalanced parentheses detected.",
                Suggestion = "Check all opening '(' have a matching closing ')'."
            });
        }

        // ── Check 3: Unclosed string literals ─────────────────────────────
        var singleQuoteCount = sql.Count(c => c == '\'');
        // Very naive check — even number expected outside of escaped quotes
        // A proper parser handles this correctly; this is a fast pre-check
        if (singleQuoteCount % 2 != 0)
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Syntax,
                Severity = IssueSeverity.Error,
                Code = "SYN004",
                Message = "Unclosed string literal (odd number of single quotes).",
                Suggestion = "Ensure all string values are properly quoted."
            });
        }

        // ── Check 4: JOIN without ON ───────────────────────────────────────
        var joinMatches = Regex.Matches(upper, @"\b(INNER|LEFT|RIGHT|FULL|CROSS)?\s*JOIN\b");
        var onCount = Regex.Matches(upper, @"\bON\b").Count;
        var crossJoins = Regex.Matches(upper, @"\bCROSS\s+JOIN\b").Count;
        var joinsNeedOn = joinMatches.Count - crossJoins;

        if (joinsNeedOn > onCount)
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Syntax,
                Severity = IssueSeverity.Warning,
                Code = "SYN005",
                Message = $"Found {joinsNeedOn} JOIN(s) but only {onCount} ON clause(s).",
                Suggestion = "Each JOIN (except CROSS JOIN) should have an ON condition."
            });
        }

        // ── Check 5: SELECT * warning ──────────────────────────────────────
        if (Regex.IsMatch(upper, @"SELECT\s+\*"))
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Syntax,
                Severity = IssueSeverity.Warning,
                Code = "SYN006",
                Message = "SELECT * detected.",
                Suggestion = "Specify explicit column names for clarity and performance."
            });
        }

        if (result.IsValid)
            result.SuggestedNextState = TurnState.RefinedQuery;

        return result;
    }
}