using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Models.SchemanticCache;
using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
using AIQueryPlatform.LLMServiceOperator.Tools;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class SemanticCacheService
    {
        private readonly SqlConnection _db;
        private readonly EmbeddingService _embeddingService;
        private const float SimilarityThreshold = 0.85f; // ← how similar to consider a hit
        private readonly string _connection;

        public SemanticCacheService(string connection, EmbeddingService embeddingService)
        {
            _db = new SqlConnection(connection);
            _embeddingService = embeddingService;
            _connection = connection;
        }

        public async Task<CacheSearchResult?> GetCacheSearchAsync(string refinedQuery, string normalizedQuery, MemoryChain currentChain)
        {
            var search = new SemanticSearch(_db, _embeddingService);
            return await search.SearchCacheAsync(refinedQuery, normalizedQuery, currentChain);
        }

        /// ── Main method to save a turn's data into the cache ───────────────────────
        public async Task SaveToCacheAsync(
      MemoryTurn turn,
      MemoryChain chain,
      string normalizedQuery)
        {
            if (string.IsNullOrEmpty(turn.GeneratedSQL)) return;

            // ── Extract parameters once; reuse everywhere ────────────────
            var extractedParams = ParameterExtractor.Extract(turn.RefinedQuery);

            // ── Parameterized templates (for matching future queries) ────
            var questionTemplate = ParameterExtractor.Templatize(turn.RefinedQuery, extractedParams);
            var normalizedTemplate = ParameterExtractor.Templatize(normalizedQuery, extractedParams);

            // ── Embed templates (not raw text) so 60%/75%/80% all match ─
            var questionVector = await _embeddingService.GetEmbeddingAsync(questionTemplate);
            var normalizedVector = await _embeddingService.GetEmbeddingAsync(normalizedTemplate);

            // ── SQL template: entity name + all parameter placeholders ───
            var sqlTemplate = turn.GeneratedSQL
                .Replace($"'{turn.ResolvedEntity?.Name}'", "{EntityName}",
                         StringComparison.OrdinalIgnoreCase);
            sqlTemplate = ParameterExtractor.Templatize(sqlTemplate, extractedParams);

            // ── Incremental SQL ──────────────────────────────────────────
            var previousSQL = chain.Turns.TakeLast(2).FirstOrDefault()?.GeneratedSQL ?? string.Empty;
            var incrementalSQL = ExtractIncrementalSQL(previousSQL, turn.GeneratedSQL);

            var record = new QueryCacheRecord
            {
                TurnLevel = turn.TurnNumber,
                QuestionText = turn.RefinedQuery,        // raw, for display
                QuestionTemplate = questionTemplate,          // ← NEW: for matching
                QuestionVector = questionVector,
                NormalizedQuestion = normalizedTemplate,
                NormalizedVector = normalizedVector,
                GeneratedSQL = turn.GeneratedSQL,
                IncrementalSQL = incrementalSQL,
                SQLTemplate = sqlTemplate,
                IncrementalFields = JsonSerializer.Serialize(ExtractFields(incrementalSQL)),
                RequiredPriorFields = JsonSerializer.Serialize(ExtractFields(previousSQL)),
                QueryType = turn.QueryType,
                DetectedEntities = JsonSerializer.Serialize(new
                {
                    Type = turn.ResolvedEntity?.Type.ToString(),
                    Name = turn.ResolvedEntity?.Name
                }),
                ExtractedParameters = JsonSerializer.Serialize(extractedParams), // ← NEW
                CreatedAt = DateTime.UtcNow
            };

            await InsertQueryCacheAsync(record);
        }

        // ── Extract what changed between two SQL strings ──────────────────
        private string ExtractIncrementalSQL(string previousSQL, string currentSQL)
        {
            if (string.IsNullOrEmpty(previousSQL))
                return currentSQL;  // Turn 1 — everything is incremental

            // Find new JOINs added in current SQL that were not in previous
            var previousJoins = ExtractJoins(previousSQL);
            var currentJoins = ExtractJoins(currentSQL);

            var newJoins = currentJoins
                .Where(j => !previousJoins.Contains(j, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Find new SELECT columns added
            var previousColumns = ExtractSelectColumns(previousSQL);
            var currentColumns = ExtractSelectColumns(currentSQL);

            var newColumns = currentColumns
                .Where(c => !previousColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Build incremental SQL representation
            var sb = new StringBuilder();
            if (newColumns.Any())
                sb.AppendLine($"NEW COLUMNS: {string.Join(", ", newColumns)}");
            if (newJoins.Any())
                sb.AppendLine($"NEW JOINS: {string.Join(" ", newJoins)}");

            return sb.ToString();
        }

        // ── Extract table/field names from SQL ───────────────────────────
        private List<string> ExtractFields(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return new List<string>();

            // Extract table names from JOINs and FROM clause
            var tableMatches = Regex.Matches(sql,
                @"\b(?:FROM|JOIN)\s+([A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.IgnoreCase);

            return tableMatches
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            // e.g. ["Students", "Attendance"]
        }

        // ── Extract JOIN clauses ──────────────────────────────────────────
        private List<string> ExtractJoins(string sql)
        {
            var matches = Regex.Matches(sql,
                @"(?:LEFT|RIGHT|INNER|OUTER)?\s*JOIN\s+\w+\s+ON\s+[^\n]+",
                RegexOptions.IgnoreCase);

            return matches
                .Select(m => m.Value.Trim())
                .ToList();
        }

        // ── Extract SELECT columns ────────────────────────────────────────
        private List<string> ExtractSelectColumns(string sql)
        {
            var match = Regex.Match(sql,
                @"SELECT\s+(.*?)\s+FROM",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success) return new List<string>();

            return match.Groups[1].Value
                .Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
        }

        public async Task InsertQueryCacheAsync(QueryCacheRecord record)
        {
            const string sql = @"
        INSERT INTO [dbo].[QueryCache]
            ([QuestionText]
            ,[QuestionTemplate]
            ,[QuestionVector]
            ,[GeneratedSQL]
            ,[QueryTypes]
            ,[DetectedEntities]
            ,[HitCount]
            ,[CreatedAt]
            ,[LastUsedAt]
            ,[NormalizedQuestion]
            ,[NormalizedVector]
            ,[SQLTemplate]
            ,[ExtractedParameters]
            ,[IncrementalSQL]
            ,[IncrementalFields]
            ,[RequiredPriorFields]
            ,[TurnLevel])
        VALUES
            (@QuestionText
            ,@QuestionTemplate
            ,@QuestionVector
            ,@GeneratedSQL
            ,@QueryTypes
            ,@DetectedEntities
            ,@HitCount
            ,@CreatedAt
            ,@LastUsedAt
            ,@NormalizedQuestion
            ,@NormalizedVector
            ,@SQLTemplate
            ,@ExtractedParameters 
            ,@IncrementalSQL
            ,@IncrementalFields
            ,@RequiredPriorFields
            ,@TurnLevel)";


            await _db.ExecuteAsync(sql, new
            {
                // ── Core Question ────────────────────────────────────────
                QuestionText = record.QuestionText,
                // e.g. "Include Attendance for Student10"

                QuestionTemplate = record.QuestionTemplate,

                QuestionVector = Helper.SerializeVector(record.QuestionVector),
                // float[] → binary/json for storage

                // ── SQL ──────────────────────────────────────────────────
                GeneratedSQL = record.GeneratedSQL,
                // Full cumulative SQL

                IncrementalSQL = record.IncrementalSQL,
                // Only what this turn added

                SQLTemplate = record.SQLTemplate,
                // SQL with '{EntityName}' placeholder

                // ── Classification ───────────────────────────────────────
                QueryTypes = record.QueryType.ToString(),
                // e.g. "Attendance"

                DetectedEntities = record.DetectedEntities,
                // e.g. {"Type":"Student","Name":"Student10"}

                ExtractedParameters = record.ExtractedParameters,
                // e.g. {"EntityType":"Student","EntityName":"string"}

                // ── Fields Tracking ──────────────────────────────────────
                IncrementalFields = record.IncrementalFields,
                // e.g. ["Attendance"]

                RequiredPriorFields = record.RequiredPriorFields,
                // e.g. ["Students"]

                TurnLevel = record.TurnLevel,
                // e.g. 2

                // ── Normalized ───────────────────────────────────────────
                NormalizedQuestion = record.NormalizedQuestion,
                // e.g. "attendance student"

                NormalizedVector = Helper.SerializeVector(record.NormalizedVector),
                // float[] → binary/json for storage

                // ── Metadata ─────────────────────────────────────────────
                HitCount = 0,               // always 0 on insert
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow  // same as CreatedAt on insert
            });
        }


    }
}
