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

        public async Task<CacheResult?> GetAsync(string userQuestion)
        {
            // 1. Extract parameters + normalize question
            var (normalized, parameters) = ParameterExtractor.Extract(userQuestion);

            // 2. Embed NORMALIZED question (without actual values)
            var normalizedVector = await _embeddingService.GetEmbeddingAsync(normalized);

            // 3. Search cache using normalized vector
            var cached = await LoadAllCacheAsync();
            if (!cached.Any()) return null;

            var best = cached
                .Where(c => c.NormalizedVector != null)
                .Select(c => (
                    cache: c,
                    similarity: CosineSimilarity(normalizedVector, c.NormalizedVector!)
                ))
                .OrderByDescending(x => x.similarity)
                .First();

            Console.WriteLine($"[Cache] Normalized match: '{best.cache.NormalizedQuestion}' | {best.similarity:F4}");

            if (best.similarity >= SimilarityThreshold)
            {
                // 4. Inject NEW parameter values into cached SQL template
                var finalSQL = InjectParameters(best.cache.SQLTemplate, parameters);

                Console.WriteLine($"[Cache] HIT ✅ Template match — injected new values");
                Console.WriteLine($"[Cache] Final SQL: {finalSQL}");

                await UpdateHitCountAsync(best.cache.CacheID);

                return new CacheResult
                {
                    SQL = finalSQL,
                    OriginalQuestion = best.cache.NormalizedQuestion,
                    Similarity = best.similarity
                };
            }

            return null;
        }

        public async Task<CacheSearchResult?> GetCacheSearchAsync(string refinedQuery,string normalizedQuery,MemoryChain currentChain)
        {
            var search = new SemanticSearch(_db, _embeddingService);
            return await search.SearchCacheAsync(refinedQuery, normalizedQuery, currentChain);
        }

        // ─── Save with normalized question + SQL template ─────────
        public async Task SaveAsync(
            string userQuestion,
            string generatedSQL,
            HashSet<string> detectedEntities)
        {
            var detector = new QueryIntentDetector();
            var queryTypes = detector.Detect(userQuestion);

            // 1. Extract parameters + normalize
            var (normalized, parameters) = ParameterExtractor.Extract(userQuestion);

            // 2. Replace values in SQL with placeholders → SQL template
            var sqlTemplate = generatedSQL;
            foreach (var p in parameters)
                sqlTemplate = sqlTemplate.Replace(p.Value.ToString()!, p.Placeholder);

            Console.WriteLine($"[Semantic Cache] SQL Template");

            // 3. Embed normalized question
            var questionVector = await _embeddingService.GetEmbeddingAsync(userQuestion);
            var normalizedVector = await _embeddingService.GetEmbeddingAsync(normalized);

            await _db.ExecuteAsync(@"
        INSERT INTO QueryCache 
            (QuestionText, QuestionVector, NormalizedQuestion, NormalizedVector,
             GeneratedSQL, SQLTemplate, QueryTypes, DetectedEntities)
        VALUES 
            (@QuestionText, @QuestionVector, @NormalizedQuestion, @NormalizedVector,
             @GeneratedSQL, @SQLTemplate, @QueryTypes, @DetectedEntities)",
                new
                {
                    QuestionText = userQuestion,
                    QuestionVector = JsonSerializer.Serialize(questionVector),
                    NormalizedQuestion = normalized,
                    NormalizedVector = JsonSerializer.Serialize(normalizedVector),
                    GeneratedSQL = generatedSQL,
                    SQLTemplate = sqlTemplate,
                    QueryTypes = string.Join(",", queryTypes),
                    DetectedEntities = string.Join(",", detectedEntities)
                });
        }

        // ─── HELPERS ──────────────────────────────────────────────
        private async Task<List<CacheEntry>> LoadAllCacheAsync()
        {
            var rows = await _db.QueryAsync<CacheEntryRaw>(@"
            SELECT 
                CacheID,
                QuestionText,
                NormalizedQuestion,
                GeneratedSQL,
                SQLTemplate,
                QuestionVector,
                NormalizedVector
            FROM QueryCache");

            return rows
                .Where(r => !string.IsNullOrEmpty(r.NormalizedVector))
                .Select(r => new CacheEntry
                {
                    CacheID = r.CacheID,
                    QuestionText = r.QuestionText,
                    NormalizedQuestion = r.NormalizedQuestion,
                    GeneratedSQL = r.GeneratedSQL,
                    SQLTemplate = r.SQLTemplate,
                    QuestionVector = JsonSerializer.Deserialize<float[]>(r.QuestionVector)
                                         ?? Array.Empty<float>(),
                    NormalizedVector = JsonSerializer.Deserialize<float[]>(r.NormalizedVector)
                                         ?? Array.Empty<float>()
                }).ToList();
        }

        private async Task UpdateHitCountAsync(int cacheID)
        {
            await _db.ExecuteAsync(@"
            UPDATE QueryCache 
            SET HitCount = HitCount + 1, LastUsedAt = GETDATE() 
            WHERE CacheID = @CacheID",
                new { CacheID = cacheID });
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
        }

        // ─── Inject new parameter values into SQL template ────────
        private string InjectParameters(string sqlTemplate, List<QueryParameter> parameters)
        {
            var sql = sqlTemplate;
            foreach (var p in parameters)
                sql = sql.Replace(p.Placeholder, p.Value.ToString());
            return sql;
        }

        public async Task SaveToCacheAsync(
        MemoryTurn turn,
        MemoryChain chain,
        string normalizedQuery)
        {
            // ── Guard: only save if SQL is valid ─────────────────────────
            if (string.IsNullOrEmpty(turn.GeneratedSQL)) return;

            // ── 1. QuestionText ──────────────────────────────────────────
            var questionText = turn.RefinedQuery;
            // e.g. "Include Attendance Information for Student10"

            // ── 2. QuestionVector ────────────────────────────────────────
            var questionVector = await _embeddingService.GetEmbeddingAsync(turn.RefinedQuery);

            // ── 3. NormalizedQuestion ────────────────────────────────────
            var normalizedQuestion = normalizedQuery;
            // e.g. "attendance student"

            // ── 4. NormalizedVector ──────────────────────────────────────
            var normalizedVector = await _embeddingService.GetEmbeddingAsync(normalizedQuery);

            // ── 5. GeneratedSQL (Cumulative) ─────────────────────────────
            // Full SQL including all prior turns joined together
            var generatedSQL = turn.GeneratedSQL;

            // ── 6. IncrementalSQL ────────────────────────────────────────
            // Only what THIS turn ADDED over previous turn
            var previousSQL = chain.Turns
                                   .TakeLast(2)
                                   .FirstOrDefault()?.GeneratedSQL ?? string.Empty;
            var incrementalSQL = ExtractIncrementalSQL(previousSQL, turn.GeneratedSQL);

            // ── 7. SQLTemplate ───────────────────────────────────────────
            // Replace entity name with placeholder for reuse
            var sqlTemplate = turn.GeneratedSQL?
                .Replace($"'{turn.ResolvedEntity?.Name}'", "{EntityName}",
                         StringComparison.OrdinalIgnoreCase);

            // ── 8. IncrementalFields ─────────────────────────────────────
            // Fields/tables ADDED in this turn only
            var incrementalFields = ExtractFields(incrementalSQL);
            // e.g. ["AttendanceDate", "AttendanceStatus"]

            // ── 9. RequiredPriorFields ───────────────────────────────────
            // Fields that must exist in session before this cache can be used
            var requiredPriorFields = ExtractFields(previousSQL);
            // e.g. ["StudentName", "DOB", "Address"]

            // ── 10. TurnLevel ────────────────────────────────────────────
            var turnLevel = turn.TurnNumber;

            // ── 11. QueryTypes ───────────────────────────────────────────
            var queryTypes = turn.QueryType;
            // e.g. "Attendance"

            // ── 12. DetectedEntities ─────────────────────────────────────
            var detectedEntities = JsonSerializer.Serialize(new
            {
                Type = turn.ResolvedEntity?.Type.ToString(),
                Name = turn.ResolvedEntity?.Name
            });
            // e.g. {"Type":"Student","Name":"Student10"}

            // ── 13. ChainID ──────────────────────────────────────────────
            var chainID = chain.ChainID;

            // ── Build record and save ────────────────────────────────────
            var record = new QueryCacheRecord
            {
                TurnLevel = turnLevel,
                QuestionText = questionText,
                QuestionVector = questionVector,
                NormalizedQuestion = normalizedQuestion,
                NormalizedVector = normalizedVector,
                GeneratedSQL = generatedSQL,
                IncrementalSQL = incrementalSQL,
                SQLTemplate = sqlTemplate,
                IncrementalFields = JsonSerializer.Serialize(incrementalFields),
                RequiredPriorFields = JsonSerializer.Serialize(requiredPriorFields),
                QueryType = queryTypes,
                DetectedEntities = detectedEntities,
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
            ,[ParameterTypes]
            ,[IncrementalSQL]
            ,[IncrementalFields]
            ,[RequiredPriorFields]
            ,[TurnLevel])
        VALUES
            (@QuestionText
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
            ,@ParameterTypes
            ,@IncrementalSQL
            ,@IncrementalFields
            ,@RequiredPriorFields
            ,@TurnLevel)";


            await _db.ExecuteAsync(sql, new
            {
                // ── Core Question ────────────────────────────────────────
                QuestionText = record.QuestionText,
                // e.g. "Include Attendance for Student10"

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

                ParameterTypes = record.ParameterTypes,
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
