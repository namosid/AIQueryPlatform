using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class QueryCacheRecord
    {
        public int CacheId { get; set; }
        // ── Core Question ─────────────────────────────────────────────
        public string QuestionText { get; set; }
        public string QuestionTemplate { get; set; }
        public float[] QuestionVector { get; set; }

        // ── SQL ───────────────────────────────────────────────────────
        public string GeneratedSQL { get; set; }
        public string? IncrementalSQL { get; set; }
        public string? SQLTemplate { get; set; }

        // ── Classification ────────────────────────────────────────────
        public QueryType QueryType { get; set; }
        public string DetectedEntities { get; set; }  // JSON
        public string? ExtractedParameters { get; set; }  // JSON

        // ── Fields Tracking ───────────────────────────────────────────
        public string? IncrementalFields { get; set; }  // JSON array
        public string? RequiredPriorFields { get; set; }  // JSON array
        public int TurnLevel { get; set; }

        // ── Normalized ────────────────────────────────────────────────
        public string NormalizedQuestion { get; set; }
        public float[] NormalizedVector { get; set; }

        // ── Metadata ──────────────────────────────────────────────────
        public int HitCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }

    public class QueryCacheRaw
    {
        public int CacheID { get; set; }
        public string QuestionText { get; set; }
        public string QuestionTemplate { get; set; }
        public string QuestionVector { get; set; }  // ← string from DB
        public string NormalizedQuestion { get; set; }
        public string NormalizedVector { get; set; }  // ← string from DB
        public string GeneratedSQL { get; set; }
        public string? IncrementalSQL { get; set; }
        public string? SQLTemplate { get; set; }
        public string QueryTypes { get; set; }
        public string DetectedEntities { get; set; }
        public string? ExtractedParameters { get; set; }
        public string? IncrementalFields { get; set; }
        public string? RequiredPriorFields { get; set; }
        public int TurnLevel { get; set; }
        public int HitCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
    }
}
