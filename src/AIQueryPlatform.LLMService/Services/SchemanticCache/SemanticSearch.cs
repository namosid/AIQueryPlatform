using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Models.SchemanticCache;
using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
using AIQueryPlatform.LLMServiceOperator.Services.STM;
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
using MatchType = AIQueryPlatform.LLMServiceOperator.Models.SchemanticCache.MatchType;

namespace AIQueryPlatform.LLMServiceOperator.Services
{

    public class SemanticSearch
    {
        private const float MinScoreThreshold = 0.85f;
        private const float ExactMatchBonus = 0.10f;
        private const float FieldMismatchPenalty = 0.20f;
        private const float TurnLevelBonus = 0.05f;
        private const int MaxCandidates = 20;

        private readonly SqlConnection _db;
        private readonly EmbeddingService _embeddingService;
        private const float SimilarityThreshold = 0.85f; // ← how similar to consider a hit

        public SemanticSearch(SqlConnection db, EmbeddingService embeddingService)
        {
            _db = db;
            _embeddingService = embeddingService;
        }
        public async Task<CacheSearchResult?> SearchCacheAsync(
        string refinedQuery,
        string normalizedQuery,
        MemoryChain currentChain)
        {
            // ── Step 1: Embed incoming query ─────────────────────────────
            var queryVector = await _embeddingService.GetEmbeddingAsync(refinedQuery);
            var normalizedVector = await _embeddingService.GetEmbeddingAsync(normalizedQuery);

            // ── Detect QueryType from refined query ──────────────────────
            var queryType = QueryTypeDetector.Detect(refinedQuery);

            // ── Step 2: Get candidates from DB by vector similarity ──────
            var candidates = await FetchCandidatesAsync(queryVector, normalizedVector, queryType);

            if (!candidates.Any()) return null;

            // ── Step 3: Get current session fields ───────────────────────
            var currentFields = currentChain != null
           ? ExtractCurrentSessionFields(currentChain)
           : new List<string>();
            if (currentChain == null)
            {
                if (candidates.Count > 0)
                    currentFields = JsonSerializer.Deserialize<List<string>>(candidates.FirstOrDefault().RequiredPriorFields);
            }


            // ── Step 4: Score and filter candidates ─────────────────────
            var scored = ScoreCandidates(candidates, queryVector, normalizedVector,
                                         currentFields, refinedQuery);

            // ── Step 5: Return best match ────────────────────────────────
            var best = scored
                .Where(s => s.FinalScore >= MinScoreThreshold)
                .OrderByDescending(s => s.FinalScore)
                .FirstOrDefault();

            if (best == null) return null;

            // ── Step 6: Update HitCount and LastUsedAt ───────────────────
            await UpdateCacheHitAsync(best.Record.CacheId);

            return new CacheSearchResult
            {
                Record = best.Record,
                FinalScore = best.FinalScore,
                MatchType = best.MatchType
            };
        }

        private async Task<List<QueryCacheRecord>> FetchCandidatesAsync(
        float[] queryVector,
        float[] normalizedVector,
        Models.STM.QueryType queryType)
        {
            // Fetch top N candidates using vector similarity from DB
            // Uses cosine similarity on both QuestionVector and NormalizedVector

            const string sql = @"
        SELECT TOP (@TopN)
             CacheID
            ,QuestionText
            ,QuestionVector
            ,NormalizedQuestion
            ,NormalizedVector
            ,GeneratedSQL
            ,IncrementalSQL
            ,SQLTemplate
            ,QueryTypes
            ,DetectedEntities
            ,ParameterTypes
            ,IncrementalFields
            ,RequiredPriorFields
            ,TurnLevel
            ,HitCount
            ,CreatedAt
            ,LastUsedAt
        FROM [dbo].[QueryCache]
        WHERE QueryTypes = @QueryType
        ORDER BY
            HitCount   DESC,
            LastUsedAt DESC";

            var results = await _db.QueryAsync<QueryCacheRaw>(sql, new
            {
                TopN = MaxCandidates,
                QueryType = queryType.ToString()
            });

            // ── Step 2: Map Raw → QueryCacheRecord (parse vectors) ───────
            var records = new List<QueryCacheRecord>();

            foreach (var r in results)
            {
                try
                {
                    records.Add(new QueryCacheRecord
                    {
                        CacheId = r.CacheID,
                        QuestionText = r.QuestionText,
                        QuestionVector = ParseVector(r.QuestionVector),      // ✅ string → float[]
                        NormalizedQuestion = r.NormalizedQuestion,
                        NormalizedVector = ParseVector(r.NormalizedVector),    // ✅ string → float[]
                        GeneratedSQL = r.GeneratedSQL,
                        IncrementalSQL = r.IncrementalSQL,
                        SQLTemplate = r.SQLTemplate,
                        QueryType = Helper.ParseQueryType(r.QueryTypes),
                        DetectedEntities = r.DetectedEntities,
                        ParameterTypes = r.ParameterTypes,
                        IncrementalFields = r.IncrementalFields,
                        RequiredPriorFields = r.RequiredPriorFields,
                        TurnLevel = r.TurnLevel,
                        HitCount = r.HitCount,
                        CreatedAt = r.CreatedAt,
                        LastUsedAt = r.LastUsedAt
                    });
                }
                catch (Exception ex)
                {
                    // Skip corrupted vector records
                    Console.WriteLine(
                        "Skipping CacheID {ID} — vector parse failed: {msg}",
                        r.CacheID, ex.Message);
                }
            }

            return records.ToList();
        }


        private float[] ParseVector(string? vectorJson)
        {
            if (string.IsNullOrWhiteSpace(vectorJson))
                return Array.Empty<float>();

            try
            {
                // Handles "[0.027526855,-0.011856079,0.00705719...]"
                return JsonSerializer.Deserialize<float[]>(vectorJson)
                       ?? Array.Empty<float>();
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private List<string> ExtractCurrentSessionFields(MemoryChain currentChain)
        {
            // ── Null guard — first turn has no chain ─────────────────────
            if (currentChain == null) return new List<string>();
            if (!currentChain.Turns.Any()) return new List<string>();

            var allFields = new List<string>();

            foreach (var turn in currentChain.Turns)
            {
                if (!string.IsNullOrEmpty(turn.GeneratedSQL))
                {
                    var fields = ExtractTablesFromSQL(turn.GeneratedSQL);
                    allFields.AddRange(fields);
                }
            }

            return allFields
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<ScoredCandidate> ScoreCandidates(List<QueryCacheRecord> candidates, float[] queryVector,
            float[] normalizedVector,
            List<string> currentFields,
            string refinedQuery)
        {
            var scored = new List<ScoredCandidate>();

            foreach (var candidate in candidates)
            {
                // ── A. Vector similarity scores ──────────────────────────
                var questionScore = CosineSimilarity(
                                           queryVector,
                                           candidate.QuestionVector);

                var normalizedScore = CosineSimilarity(
                                          normalizedVector,
                                          candidate.NormalizedVector);

                // Take best of two similarity scores
                var baseScore = Math.Max(questionScore, normalizedScore);

                // ── B. Skip low base scores immediately ──────────────────
                if (baseScore < MinScoreThreshold) continue;

                // ── C. Required prior fields check ───────────────────────
                var requiredFields = DeserializeJsonArray(candidate.RequiredPriorFields);
                bool fieldsMatch = requiredFields
                                        .All(f => currentFields.Contains(f,
                                             StringComparer.OrdinalIgnoreCase));

                // Hard block — required fields not in current session
                if (!fieldsMatch)
                {
                    scored.Add(new ScoredCandidate
                    {
                        Record = candidate,
                        FinalScore = 0f,        // blocked
                        MatchType = MatchType.Blocked
                    });
                    continue;
                }

                // ── D. Bonus: exact text match ───────────────────────────
                float exactBonus = string.Equals(
                                       candidate.NormalizedQuestion,
                                       refinedQuery.ToLower().Trim(),
                                       StringComparison.OrdinalIgnoreCase)
                                   ? ExactMatchBonus : 0f;

                // ── E. Bonus: turn level proximity ───────────────────────
                // Prefer cache records from similar turn depth
                int currentTurnLevel = currentFields.Count + 1;
                float turnBonus = candidate.TurnLevel == currentTurnLevel
                                         ? TurnLevelBonus : 0f;

                // ── F. Final score ───────────────────────────────────────
                var finalScore = baseScore + exactBonus + turnBonus;

                scored.Add(new ScoredCandidate
                {
                    Record = candidate,
                    FinalScore = finalScore,
                    MatchType = finalScore >= MinScoreThreshold
                                 ? MatchType.Hit
                                 : MatchType.Miss
                });
            }

            return scored;
        }

        private async Task UpdateCacheHitAsync(int cacheID)
        {
            const string sql = @"
            UPDATE [dbo].[QueryCache]
            SET
                HitCount   = HitCount + 1,
                LastUsedAt = @LastUsedAt
            WHERE CacheID  = @CacheID";


            await _db.ExecuteAsync(sql, new
            {
                CacheID = cacheID,
                LastUsedAt = DateTime.UtcNow
            });
        }

        // ── Cosine Similarity ─────────────────────────────────────────────
        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            // ── Guard: empty or mismatched vectors ───────────────────────
            if (vectorA == null || vectorB == null) return 0f;
            if (vectorA.Length == 0 || vectorB.Length == 0) return 0f;
            if (vectorA.Length != vectorB.Length) return 0f;

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            float denominator = MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB);

            return denominator == 0f ? 0f : dotProduct / denominator;
        }

        // ── Extract Tables from SQL ───────────────────────────────────────
        private List<string> ExtractTablesFromSQL(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return new List<string>();

            var matches = Regex.Matches(sql,
                @"\b(?:FROM|JOIN)\s+([A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.IgnoreCase);

            return matches
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ── Deserialize JSON Array ────────────────────────────────────────
        private List<string> DeserializeJsonArray(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(json)
                   ?? new List<string>();
        }

        // ── Serialize Vector ──────────────────────────────────────────────
        private byte[] SerializeVector(float[] vector)
        {
            var bytes = new byte[vector.Length * 4];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        // ── Deserialize Vector ────────────────────────────────────────────
        private float[] DeserializeVector(byte[] bytes)
        {
            var vector = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            return vector;
        }
    }
}
