using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AIQueryPlatform.SqlValidator.Models;
using AIQueryPlatform.SqlValidator.Validators;
using AIQueryPlatform.SqlValidator.Services;

namespace AIQueryPlatform.SqlValidator.Services;

/// <summary>
/// Orchestrates all 4 validation layers in sequence.
/// Each layer only runs if the previous passes (or is warnings-only).
/// Feeds structured results back into the STM as the correct next state.
/// </summary>
public class SqlValidationPipeline
{
    private readonly SyntaxValidator _syntaxValidator;
    private readonly SchemaValidator _schemaValidator;
    private readonly LLMSelfCheckService _llmSelfCheck;
    private readonly DryRunValidator? _dryRunValidator; // optional — needs DB

    public SqlValidationPipeline(
        DatabaseSchema schema,
        HttpClient httpClient,
        string? connectionString = null)
    {
        _syntaxValidator = new SyntaxValidator();
        _schemaValidator = new SchemaValidator(schema);
        _llmSelfCheck = new LLMSelfCheckService(httpClient);
        _dryRunValidator = connectionString is not null
            ? new DryRunValidator(connectionString)
            : null;
    }

    /// <summary>
    /// Run all validation layers and return a unified pipeline result.
    /// </summary>
    public async Task<PipelineResult> ValidateAsync(
        string sql,
        STMTurn currentTurn,
        CancellationToken ct = default)
    {
        var pipeline = new PipelineResult { SQL = sql, TurnNumber = currentTurn.TurnNumber };

        // ── Layer 1: Syntax ────────────────────────────────────────────────
        Console.WriteLine("[Pipeline] Layer 1: Syntax validation...");
        var syntaxResult = _syntaxValidator.Validate(sql);
        pipeline.LayerResults[ValidationLayer.Syntax] = syntaxResult;

        if (syntaxResult.Errors.Any())
        {
            pipeline.FinalState = TurnState.SQLError;
            pipeline.FailedLayer = ValidationLayer.Syntax;
            pipeline.FixHint = BuildFixHint(syntaxResult);
            return pipeline;
        }

        // ── Layer 2: Schema ────────────────────────────────────────────────
        Console.WriteLine("[Pipeline] Layer 2: Schema validation...");
        var schemaResult = _schemaValidator.Validate(sql);
        pipeline.LayerResults[ValidationLayer.Schema] = schemaResult;

        if (schemaResult.Errors.Any())
        {
            pipeline.FinalState = TurnState.SQLError;
            pipeline.FailedLayer = ValidationLayer.Schema;
            pipeline.FixHint = BuildFixHint(schemaResult);
            return pipeline;
        }

        //// ── Layer 3: LLM Self-Check ────────────────────────────────────────
        //Console.WriteLine("[Pipeline] Layer 3: LLM self-check...");
        //var llmResult = await _llmSelfCheck.ValidateAsync(sql, ExtractSchema(), ct);
        //pipeline.LayerResults[ValidationLayer.LLMSelfCheck] = llmResult;

        //if (llmResult.Errors.Any())
        //{
        //    pipeline.FinalState = TurnState.SQLError;
        //    pipeline.FailedLayer = ValidationLayer.LLMSelfCheck;
        //    pipeline.FixHint = BuildFixHint(llmResult);
        //    return pipeline;
        //}

        // ── Layer 4: Dry Run (optional) ────────────────────────────────────
        if (_dryRunValidator is not null)
        {
            Console.WriteLine("[Pipeline] Layer 4: Dry run (EXPLAIN)...");
            var dryRunResult = await _dryRunValidator.ValidateAsync(sql, ct);
            pipeline.LayerResults[ValidationLayer.DryRun] = dryRunResult;

            if (dryRunResult.Errors.Any())
            {
                pipeline.FinalState = TurnState.SQLError;
                pipeline.FailedLayer = ValidationLayer.DryRun;
                pipeline.FixHint = BuildFixHint(dryRunResult);
                return pipeline;
            }
        }

        // ── All layers passed ──────────────────────────────────────────────
        pipeline.FinalState = TurnState.RefinedQuery;
        Console.WriteLine("[Pipeline] ✓ All validation layers passed. Query is ready to execute.");
        return pipeline;
    }

    // ── Build a structured fix hint to feed back into STM ─────────────────────

    private static string BuildFixHint(ValidationResult result)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SQL validation failed. Issues found:");

        foreach (var issue in result.Errors)
        {
            sb.AppendLine($"  [{issue.Code}] {issue.Message}");
            if (issue.Suggestion is not null)
                sb.AppendLine($"  → Fix: {issue.Suggestion}");
        }

        foreach (var warn in result.Warnings)
        {
            sb.AppendLine($"  [WARN:{warn.Code}] {warn.Message}");
            if (warn.Suggestion is not null)
                sb.AppendLine($"  → Suggestion: {warn.Suggestion}");
        }

        return sb.ToString();
    }

    // Placeholder — in real use, inject the schema directly
    private DatabaseSchema ExtractSchema() => new();
}

// ── Pipeline Result ───────────────────────────────────────────────────────────

public class PipelineResult
{
    public string SQL { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public TurnState FinalState { get; set; }
    public ValidationLayer? FailedLayer { get; set; }
    public string? FixHint { get; set; }
    public bool IsValid => FinalState == TurnState.RefinedQuery;

    public Dictionary<ValidationLayer, ValidationResult> LayerResults { get; } = [];

    public IEnumerable<ValidationIssue> AllIssues =>
        LayerResults.Values.SelectMany(r => r.Issues);

    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine($"  Validation Result: {(IsValid ? "✓ PASSED" : "✗ FAILED")}");
        Console.WriteLine($"  Turn: {TurnNumber}  |  Next STM State: {FinalState}");
        if (FailedLayer.HasValue)
            Console.WriteLine($"  Failed at Layer: {FailedLayer}");
        Console.WriteLine("══════════════════════════════════════════");

        foreach (var (layer, result) in LayerResults)
        {
            Console.WriteLine($"\n  [{layer}]");
            foreach (var issue in result.Issues)
            {
                var icon = issue.Severity switch
                {
                    IssueSeverity.Error => "✗",
                    IssueSeverity.Warning => "⚠",
                    _ => "ℹ"
                };
                Console.WriteLine($"    {icon} [{issue.Code}] {issue.Message}");
                if (issue.Suggestion is not null)
                    Console.WriteLine($"       → {issue.Suggestion}");
            }
        }

        if (FixHint is not null)
        {
            Console.WriteLine("\n  ── Fix hint for STM re-prompt ──");
            Console.WriteLine($"  {FixHint.Replace("\n", "\n  ")}");
        }
    }
}