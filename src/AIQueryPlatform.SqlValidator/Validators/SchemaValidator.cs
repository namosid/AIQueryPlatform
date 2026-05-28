using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AIQueryPlatform.SqlValidator.Models;

namespace AIQueryPlatform.SqlValidator.Validators;

/// <summary>
/// Layer 2 — Schema-Aware Validation.
/// Validates tables, columns, and JOIN keys against your known schema.
/// Feed this the DatabaseSchema built from your actual DB metadata.
/// </summary>
public class SchemaValidator(DatabaseSchema schema)
{
    // Matches: FROM TableName [alias]  or  JOIN TableName [alias]
    private static readonly Regex TableRefRegex = new(
        @"\b(?:FROM|JOIN)\s+(\[?\w+\]?)(?:\s+(?:AS\s+)?(\w+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches: alias.ColumnName  or  TableName.ColumnName
    private static readonly Regex QualifiedColumnRegex = new(
        @"\b(\w+)\.(\w+)\b",
        RegexOptions.Compiled);

    // Matches bare column names in SELECT (very simplified — use AST for production)
    private static readonly Regex SelectColumnRegex = new(
        @"SELECT\s+([\s\S]+?)\s+FROM",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ValidationResult Validate(string sql)
    {
        var result = new ValidationResult
        {
            SuggestedNextState = TurnState.SQLError
        };

        var upper = sql.ToUpperInvariant();

        // ── Build alias → table map ────────────────────────────────────────
        var aliasMap = BuildAliasMap(sql);

        // ── Check 1: All referenced tables exist ──────────────────────────
        var missingTables = new List<string>();
        foreach (var (tableName, _) in aliasMap)
        {
            if (schema.GetTable(tableName) is null)
                missingTables.Add(tableName);
        }

        foreach (var t in missingTables)
        {
            var suggestion = SuggestSimilar(t, schema.Tables.Select(x => x.TableName));
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Schema,
                Severity = IssueSeverity.Error,
                Code = "SCH001",
                Message = $"Table '{t}' does not exist in the schema.",
                Suggestion = suggestion is not null
                    ? $"Did you mean '{suggestion}'?"
                    : "Check the table name or add it to the schema definition."
            });
        }

        // ── Check 2: Qualified columns (alias.Column) exist ───────────────
        foreach (Match match in QualifiedColumnRegex.Matches(sql))
        {
            var prefix = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            // Resolve alias → actual table name
            var tableName = aliasMap
                .FirstOrDefault(kv => kv.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                .Key ?? prefix;

            var tableSchema = schema.GetTable(tableName);
            if (tableSchema is null) continue; // already flagged in Check 1

            if (tableSchema.GetColumn(columnName) is null)
            {
                // Check if this column exists in another table in the schema
                var columnInOtherTable = schema.Tables
                    .Where(t => !t.TableName.Equals(tableSchema.TableName, StringComparison.OrdinalIgnoreCase))
                    .Select(t => new { Table = t, Column = t.GetColumn(columnName) })
                    .FirstOrDefault(x => x.Column is not null);

                string suggestion;

                if (columnInOtherTable is not null)
                {
                    // Column exists in another table — check if that table is already joined
                    var isAlreadyJoined = aliasMap.Keys.Any(k =>
                        k.Equals(columnInOtherTable.Table.TableName, StringComparison.OrdinalIgnoreCase));

                    suggestion = isAlreadyJoined
                        ? $"'{columnName}' belongs to '{columnInOtherTable.Table.TableName}' which is already joined. " +
                          $"Use the correct alias: e.g. qualify as '{columnInOtherTable.Table.TableName}.{columnName}'."
                        : $"'{columnName}' does not belong to '{tableSchema.TableName}'. " +
                          $"It exists in '{columnInOtherTable.Table.TableName}' — " +
                          $"add: LEFT JOIN {columnInOtherTable.Table.TableName} ON " +
                          $"{BuildJoinHint(tableSchema, columnInOtherTable.Table)}.";
                }
                else
                {
                    // Column not found anywhere — fall back to similar name suggestion
                    var similar = SuggestSimilar(columnName, tableSchema.Columns.Select(c => c.ColumnName));
                    suggestion = similar is not null
                        ? $"Did you mean '{tableSchema.TableName}.{similar}'?"
                        : $"'{columnName}' was not found in any table in the schema.";
                }

                var issue = new ValidationIssue
                {
                    Layer = ValidationLayer.Schema,
                    Severity = IssueSeverity.Error,
                    Code = "SCH002",
                    Message = $"Column '{columnName}' does not exist in table '{tableSchema.TableName}'.",
                    Suggestion = suggestion
                };
                if (IsAddIssue(issue, result.Issues))
                {
                    result.Issues.Add(issue);
                }

            }
        }

        // ── Check 3: JOIN ON key types are compatible ──────────────────────
        ValidateJoinKeys(sql, aliasMap, result);

        // ── Check 4: WHERE clause references valid columns ─────────────────
        ValidateWhereColumns(sql, aliasMap, result);

        // ── Check 5: ORDER BY columns exist ───────────────────────────────
        ValidateOrderByColumns(sql, aliasMap, result);

        if (result.IsValid)
            result.SuggestedNextState = TurnState.RefinedQuery;

        return result;
    }

    public bool IsAddIssue(ValidationIssue issue, List<ValidationIssue> Issues)
    {
        var isDuplicate = Issues.Any(existing =>
            existing.Code == issue.Code &&
            existing.Message.Equals(issue.Message, StringComparison.OrdinalIgnoreCase));

        if (!isDuplicate)
            return true;
        else
            return false;
    }
    // ── Helpers ───────────────────────────────────────────────────────────────

    private Dictionary<string, string> BuildAliasMap(string sql)
    {
        // Key = actual table name, Value = alias (or table name if no alias)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in TableRefRegex.Matches(sql))
        {
            var tableName = m.Groups[1].Value.Trim('[', ']');
            var alias = m.Groups[2].Success ? m.Groups[2].Value : tableName;
            map.TryAdd(tableName, alias);
        }
        return map;
    }

    private void ValidateJoinKeys(
        string sql,
        Dictionary<string, string> aliasMap,
        ValidationResult result)
    {
        // Pattern: ON a.ColA = b.ColB
        var joinOnRegex = new Regex(
            @"\bON\s+(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)",
            RegexOptions.IgnoreCase);

        foreach (Match m in joinOnRegex.Matches(sql))
        {
            CheckJoinColumn(m.Groups[1].Value, m.Groups[2].Value, aliasMap, result);
            CheckJoinColumn(m.Groups[3].Value, m.Groups[4].Value, aliasMap, result);
        }
    }

    private void CheckJoinColumn(
        string prefix,
        string column,
        Dictionary<string, string> aliasMap,
        ValidationResult result)
    {
        var tableName = aliasMap
            .FirstOrDefault(kv => kv.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            .Key ?? prefix;

        var tableSchema = schema.GetTable(tableName);
        if (tableSchema is null) return;

        if (tableSchema.GetColumn(column) is null)
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.Schema,
                Severity = IssueSeverity.Error,
                Code = "SCH003",
                Message = $"JOIN key '{column}' does not exist in '{tableSchema.TableName}'.",
                Suggestion = $"Valid columns: {string.Join(", ", tableSchema.Columns.Select(c => c.ColumnName))}"
            });
        }
    }

    private void ValidateWhereColumns(
        string sql,
        Dictionary<string, string> aliasMap,
        ValidationResult result)
    {
        var whereMatch = Regex.Match(sql, @"\bWHERE\b([\s\S]+?)(?:\bORDER\b|\bGROUP\b|\bHAVING\b|$)",
            RegexOptions.IgnoreCase);

        if (!whereMatch.Success) return;

        foreach (Match m in QualifiedColumnRegex.Matches(whereMatch.Groups[1].Value))
        {
            var prefix = m.Groups[1].Value;
            var col = m.Groups[2].Value;

            var tableName = aliasMap
                .FirstOrDefault(kv => kv.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                .Key ?? prefix;

            var tableSchema = schema.GetTable(tableName);
            if (tableSchema is null) continue;

            if (tableSchema.GetColumn(col) is null)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.Schema,
                    Severity = IssueSeverity.Error,
                    Code = "SCH004",
                    Message = $"WHERE column '{col}' not found in '{tableSchema.TableName}'.",
                    Suggestion = SuggestSimilar(col, tableSchema.Columns.Select(c => c.ColumnName))
                        is string s ? $"Did you mean '{s}'?" : null
                });
            }
        }
    }

    private void ValidateOrderByColumns(
        string sql,
        Dictionary<string, string> aliasMap,
        ValidationResult result)
    {
        var orderByMatch = Regex.Match(sql, @"\bORDER\s+BY\b([\s\S]+?)(?:;|$)",
            RegexOptions.IgnoreCase);

        if (!orderByMatch.Success) return;

        foreach (Match m in QualifiedColumnRegex.Matches(orderByMatch.Groups[1].Value))
        {
            var prefix = m.Groups[1].Value;
            var col = m.Groups[2].Value;

            var tableName = aliasMap
                .FirstOrDefault(kv => kv.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                .Key ?? prefix;

            var tableSchema = schema.GetTable(tableName);
            if (tableSchema is null) continue;

            if (tableSchema.GetColumn(col) is null)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.Schema,
                    Severity = IssueSeverity.Error,
                    Code = "SCH005",
                    Message = $"ORDER BY column '{col}' not found in '{tableSchema.TableName}'.",
                    Suggestion = null
                });
            }
        }
    }

    /// <summary>
    /// Simple Levenshtein-based "did you mean?" suggester.
    /// </summary>
    private static string? SuggestSimilar(string input, IEnumerable<string> candidates)
    {
        var best = string.Empty;
        var bestScore = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var score = LevenshteinDistance(
                input.ToUpperInvariant(),
                candidate.ToUpperInvariant());

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        // Only suggest if reasonably close
        return bestScore <= 3 ? best : null;
    }

    /// <summary>
    /// Builds a JOIN hint by finding the FK relationship between two tables.
    /// e.g.  "c.ClassID = s.ClassID"
    /// </summary>
    private static string BuildJoinHint(TableSchema fromTable, TableSchema targetTable)
    {
        // Look for FK on fromTable pointing to targetTable
        var fk = fromTable.ForeignKeys
            .FirstOrDefault(f => f.ReferencedTable.Equals(
                targetTable.TableName, StringComparison.OrdinalIgnoreCase));

        if (fk is not null)
            return $"{targetTable.TableName[0]}.{fk.ReferencedColumn} = " +
                   $"{fromTable.TableName[0]}.{fk.ColumnName}";

        // Look for FK on targetTable pointing to fromTable
        var reverseFk = targetTable.ForeignKeys
            .FirstOrDefault(f => f.ReferencedTable.Equals(
                fromTable.TableName, StringComparison.OrdinalIgnoreCase));

        if (reverseFk is not null)
            return $"{targetTable.TableName[0]}.{reverseFk.ColumnName} = " +
                   $"{fromTable.TableName[0]}.{reverseFk.ReferencedColumn}";

        // No FK found — generic hint
        return $"{targetTable.TableName[0]}./* join key */ = " +
               $"{fromTable.TableName[0]}./* join key */";
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }

        return dp[a.Length, b.Length];
    }


}