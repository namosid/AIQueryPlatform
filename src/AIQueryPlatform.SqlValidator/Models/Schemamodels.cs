using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.SqlValidator.Models;

// ─── Schema Definition ────────────────────────────────────────────────────────

public class DatabaseSchema
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<TableSchema> Tables { get; set; } = [];

    public TableSchema? GetTable(string tableName) =>
        Tables.FirstOrDefault(t =>
            t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
}

public class TableSchema
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnSchema> Columns { get; set; } = [];
    public List<ForeignKey> ForeignKeys { get; set; } = [];

    public ColumnSchema? GetColumn(string columnName) =>
        Columns.FirstOrDefault(c =>
            c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
}

public class ColumnSchema
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
    public bool IsForeignKey { get; set; } = false;
}

public class ForeignKey
{
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
}

// ─── STM Turn Models ──────────────────────────────────────────────────────────

public enum TurnState
{
    FreshQuery,         // new unrelated question
    ClarificationAsked, // system asked for more info
    ClarificationGiven, // user answered clarification
    RefinedQuery,       // query refined from context
    FollowUp,           // refers to previous turn
    SQLError,
    SQLErrorResolved
}

public class STMTurn
{
    public int TurnNumber { get; set; }
    public TurnState State { get; set; }
    public string UserInput { get; set; } = string.Empty;
    public string? GeneratedSQL { get; set; }
    public string? ErrorMessage { get; set; }
}

// ─── Validation Result Models ─────────────────────────────────────────────────

public enum ValidationLayer
{
    Syntax,
    Schema,
    LLMSelfCheck,
    DryRun
}

public enum IssueSeverity
{
    Error,
    Warning,
    Info
}

public class ValidationIssue
{
    public ValidationLayer Layer { get; set; }
    public IssueSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
}

public class ValidationResult
{
    public bool IsValid => Issues.All(i => i.Severity != IssueSeverity.Error);
    public List<ValidationIssue> Issues { get; set; } = [];
    public TurnState SuggestedNextState { get; set; }
    public string? FixHint { get; set; }

    public IEnumerable<ValidationIssue> Errors =>
        Issues.Where(i => i.Severity == IssueSeverity.Error);

    public IEnumerable<ValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == IssueSeverity.Warning);
}