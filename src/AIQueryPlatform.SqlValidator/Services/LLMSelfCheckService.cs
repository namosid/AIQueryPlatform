using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIQueryPlatform.SqlValidator.Models;

namespace AIQueryPlatform.SqlValidator.Services;

/// <summary>
/// Layer 3 — LLM Self-Check.
/// Sends the generated SQL + schema back to an LLM and asks it to
/// identify semantic issues a parser cannot catch.
/// Requires: HttpClient with Anthropic API key in Authorization header.
/// </summary>
public class LLMSelfCheckService(HttpClient httpClient)
{
    private const string AnthropicModel = "claude-sonnet-4-20250514";
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";

    public async Task<ValidationResult> ValidateAsync(
        string sql,
        DatabaseSchema schema,
        CancellationToken ct = default)
    {
        var result = new ValidationResult
        {
            SuggestedNextState = TurnState.SQLError
        };

        var schemaDescription = BuildSchemaDescription(schema);
        var prompt = BuildPrompt(sql, schemaDescription);

        try
        {
            var request = new AnthropicRequest
            {
                Model = AnthropicModel,
                MaxTokens = 1024,
                Messages =
                [
                    new() { Role = "user", Content = prompt }
                ]
            };

            var response = await httpClient.PostAsJsonAsync(AnthropicApiUrl, request, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<AnthropicResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var text = parsed?.Content?.FirstOrDefault()?.Text ?? string.Empty;
            ParseLLMResponse(text, result);
        }
        catch (Exception ex)
        {
            // LLM check failure is non-blocking — log and continue
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.LLMSelfCheck,
                Severity = IssueSeverity.Warning,
                Code = "LLM000",
                Message = $"LLM self-check could not complete: {ex.Message}",
                Suggestion = "Proceed to dry-run validation."
            });
        }

        if (result.IsValid)
            result.SuggestedNextState = TurnState.RefinedQuery;

        return result;
    }

    // ── Prompt Builder ────────────────────────────────────────────────────────

    private static string BuildPrompt(string sql, string schemaDescription) => $$"""
        You are a SQL validation assistant. Review the SQL query below against the provided schema.

        SCHEMA:
        {schemaDescription}

        SQL QUERY:
        {sql}

        Identify ALL issues. For each issue, respond ONLY in this exact JSON format (array of objects):
        [
          {
            "severity": "error" | "warning" | "info",
            "code": "short code e.g. SEM001",
            "message": "clear description of the issue",
            "suggestion": "how to fix it"
          }
        ]

        Check for:
        1. Tables referenced but not joined (missing JOIN)
        2. Columns that are ambiguous (same name in multiple joined tables without alias)
        3. JOIN conditions that look logically wrong (wrong FK relationship)
        4. Missing WHERE filters that are likely required based on context
        5. Aggregations used without GROUP BY
        6. Columns used in WHERE/ORDER BY that aren't in SELECT and may cause issues
        7. Any semantic mismatch between what the query does vs what it appears to intend

        If there are NO issues, respond with an empty array: []
        Respond ONLY with the JSON array. No preamble, no explanation outside JSON.
        """;

    private static string BuildSchemaDescription(DatabaseSchema schema)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {table.TableName}");
            foreach (var col in table.Columns)
            {
                var flags = new List<string>();
                if (col.IsPrimaryKey) flags.Add("PK");
                if (col.IsForeignKey) flags.Add("FK");
                if (!col.IsNullable) flags.Add("NOT NULL");
                var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
                sb.AppendLine($"  - {col.ColumnName} ({col.DataType}){flagStr}");
            }
            foreach (var fk in table.ForeignKeys)
                sb.AppendLine($"  FK: {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ── Response Parser ───────────────────────────────────────────────────────

    private static void ParseLLMResponse(string text, ValidationResult result)
    {
        try
        {
            // Strip any accidental markdown fences
            var json = text
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            if (string.IsNullOrEmpty(json) || json == "[]") return;

            var issues = JsonSerializer.Deserialize<List<LLMIssueDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (issues is null) return;

            foreach (var issue in issues)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Layer = ValidationLayer.LLMSelfCheck,
                    Severity = issue.Severity?.ToLowerInvariant() switch
                    {
                        "error" => IssueSeverity.Error,
                        "warning" => IssueSeverity.Warning,
                        _ => IssueSeverity.Info
                    },
                    Code = issue.Code ?? "LLM001",
                    Message = issue.Message ?? string.Empty,
                    Suggestion = issue.Suggestion
                });
            }
        }
        catch
        {
            // If we can't parse the LLM response, treat as a warning, not a failure
            result.Issues.Add(new ValidationIssue
            {
                Layer = ValidationLayer.LLMSelfCheck,
                Severity = IssueSeverity.Warning,
                Code = "LLM002",
                Message = "LLM self-check returned an unparseable response.",
                Suggestion = "Review the SQL manually or retry the LLM check."
            });
        }
    }

    // ── Anthropic API DTOs ────────────────────────────────────────────────────

    private sealed class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; } = [];
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContent>? Content { get; set; }
    }

    private sealed class AnthropicContent
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class LLMIssueDto
    {
        public string? Severity { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? Suggestion { get; set; }
    }
}