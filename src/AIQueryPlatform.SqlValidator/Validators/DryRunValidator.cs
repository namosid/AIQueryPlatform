using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using AIQueryPlatform.SqlValidator.Models;

namespace AIQueryPlatform.SqlValidator.Validators;

/// <summary>
/// Layer 4 — Dry Run / EXPLAIN validation.
/// Uses SET SHOWPLAN_ALL ON (SQL Server) to validate the query plan
/// without executing it — no data is read or written.
///
/// NuGet required: Microsoft.Data.SqlClient
/// </summary>
public class DryRunValidator(string connectionString)
{
    public async Task<ValidationResult> ValidateAsync(
        string sql,
        CancellationToken ct = default)
    {
        var result = new ValidationResult
        {
            SuggestedNextState = TurnState.SQLError
        };

        await using var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(ct);

            // SET SHOWPLAN_ALL ON causes SQL Server to return the execution plan
            // instead of executing the query — safe, no side effects
            await using var setPlan = new SqlCommand("SET SHOWPLAN_ALL ON", connection);
            await setPlan.ExecuteNonQueryAsync(ct);

            try
            {
                await using var cmd = new SqlCommand(sql, connection)
                {
                    CommandTimeout = 10
                };

                // Read the plan — this validates the query fully against the live DB
                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // If we get here, the query is valid
                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.DryRun,
                    Severity = IssueSeverity.Info,
                    Code = "DRY000",
                    Message = "Query plan generated successfully. Query is valid.",
                    Suggestion = null
                });

                // Optional: parse the plan for warnings (missing indexes, etc.)
                await ParseExecutionPlan(reader, result, ct);
            }
            catch (SqlException sqlEx)
            {
                ParseSqlException(sqlEx, result);
            }
            finally
            {
                // Always turn SHOWPLAN off after validation
                await using var unsetPlan = new SqlCommand("SET SHOWPLAN_ALL OFF", connection);
                await unsetPlan.ExecuteNonQueryAsync(ct);
            }
        }
        catch (SqlException connEx) when (connEx.Number is 4060 or 18456 or 10061)
        {
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.DryRun,
                Severity = IssueSeverity.Warning,
                Code = "DRY001",
                Message = "Cannot connect to database for dry-run validation.",
                Suggestion = "Check connection string. Falling back on schema validation only."
            });
        }

        if (result.IsValid)
            result.SuggestedNextState = TurnState.RefinedQuery;

        return result;
    }

    // ── SQL Exception → Structured Issues ─────────────────────────────────────

    private static void ParseSqlException(SqlException ex, ValidationResult result)
    {
        foreach (SqlError error in ex.Errors)
        {
            // Map SQL Server error numbers to meaningful codes
            var (code, suggestion) = error.Number switch
            {
                208 => ("DRY208", "Table does not exist. Check table name and schema."),
                207 => ("DRY207", "Invalid column name. Check column exists in the referenced table."),
                209 => ("DRY209", "Ambiguous column name. Qualify with table alias (e.g. s.ColumnName)."),
                4104 => ("DRY4104", "Multi-part identifier could not be bound. Check alias and table references."),
                156 => ("DRY156", "Incorrect syntax near keyword."),
                102 => ("DRY102", "Incorrect syntax. Check for missing commas or keywords."),
                _ => ($"DRY{error.Number}", "Review the SQL and schema for this error.")
            };

            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.DryRun,
                Severity = IssueSeverity.Error,
                Code = code,
                Message = $"[SQL Server {error.Number}] {error.Message}",
                Suggestion = suggestion
            });
        }
    }

    // ── Execution Plan Parser (optional performance warnings) ─────────────────

    private static async Task ParseExecutionPlan(
        SqlDataReader reader,
        ValidationResult result,
        CancellationToken ct)
    {
        // SHOWPLAN_ALL returns columns including Warnings and PhysicalOp
        // We look for table scans (missing indexes) as a performance warning
        while (await reader.ReadAsync(ct))
        {
            var physicalOp = reader.IsDBNull(reader.GetOrdinal("PhysicalOp"))
                ? null
                : reader.GetString(reader.GetOrdinal("PhysicalOp"));

            if (physicalOp is "Table Scan" or "Clustered Index Scan")
            {
                var obj = reader.IsDBNull(reader.GetOrdinal("Argument"))
                    ? "unknown table"
                    : reader.GetString(reader.GetOrdinal("Argument"));

                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.DryRun,
                    Severity = IssueSeverity.Warning,
                    Code = "DRY_PERF01",
                    Message = $"Full table scan detected on {obj}.",
                    Suggestion = "Consider adding an index on the filtered/joined columns."
                });
            }
        }
    }
}