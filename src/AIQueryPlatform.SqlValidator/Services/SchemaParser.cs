using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using AIQueryPlatform.SqlValidator.Models;

namespace AIQueryPlatform.SqlValidator.Services;

/// <summary>
/// Parses a plain-text LLM schema string into DatabaseSchema model.
///
/// Expected format:
///   Table: TableName
///     - ColumnName (datatype) [NOT NULL]
///   PK: ColumnName
///   FK: ColumnName → ReferencedTable.ReferencedColumn
/// </summary>
public static class SchemaParser
{
    // Regex patterns
    private static readonly Regex TableRegex = new(@"^Table:\s*(\w+)", RegexOptions.Compiled);
    private static readonly Regex ColumnRegex = new(@"^\s*-\s*(\w+)\s*\((\w+)\)\s*(NOT NULL)?", RegexOptions.Compiled);
    private static readonly Regex PkRegex = new(@"^\s*PK:\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex FkRegex = new(@"^\s*FK:\s*(\w+)\s*[→\-\>]+\s*(\w+)\.(\w+)", RegexOptions.Compiled);

    public static DatabaseSchema Parse(string schemaString, string databaseName = "Database")
    {
        var database = new DatabaseSchema { DatabaseName = databaseName };

        TableSchema? currentTable = null;

        foreach (var rawLine in schemaString.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // ── Table header ───────────────────────────────────────────────
            var tableMatch = TableRegex.Match(line);
            if (tableMatch.Success)
            {
                currentTable = new TableSchema { TableName = tableMatch.Groups[1].Value.Trim() };
                database.Tables.Add(currentTable);
                continue;
            }

            if (currentTable is null) continue;

            // ── Column line ────────────────────────────────────────────────
            var colMatch = ColumnRegex.Match(line);
            if (colMatch.Success)
            {
                currentTable.Columns.Add(new ColumnSchema
                {
                    ColumnName = colMatch.Groups[1].Value.Trim(),
                    DataType = colMatch.Groups[2].Value.Trim(),
                    IsNullable = !colMatch.Groups[3].Success   // NOT NULL → IsNullable = false
                });
                continue;
            }

            // ── PK line  (PK: Col1, Col2) ──────────────────────────────────
            var pkMatch = PkRegex.Match(line);
            if (pkMatch.Success)
            {
                var pkColumns = pkMatch.Groups[1].Value
                    .Split(',', StringSplitOptions.TrimEntries);

                foreach (var pkCol in pkColumns)
                {
                    var col = currentTable.GetColumn(pkCol);
                    if (col is not null)
                    {
                        col.IsPrimaryKey = true;
                        col.IsNullable = false; // PKs are never nullable
                    }
                }
                continue;
            }

            // ── FK line  (FK: ColName → RefTable.RefColumn) ────────────────
            var fkMatch = FkRegex.Match(line);
            if (fkMatch.Success)
            {
                var fkColName = fkMatch.Groups[1].Value.Trim();
                var refTable = fkMatch.Groups[2].Value.Trim();
                var refCol = fkMatch.Groups[3].Value.Trim();

                // Mark the column as FK
                var col = currentTable.GetColumn(fkColName);
                if (col is not null)
                    col.IsForeignKey = true;

                currentTable.ForeignKeys.Add(new ForeignKey
                {
                    ColumnName = fkColName,
                    ReferencedTable = refTable,
                    ReferencedColumn = refCol
                });
            }
        }

        return database;
    }
}
