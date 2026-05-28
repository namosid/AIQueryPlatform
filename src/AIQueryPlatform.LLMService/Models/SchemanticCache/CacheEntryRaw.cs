using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    // ─── Raw DB row before deserialization ────────────────────
    public class CacheEntryRaw
    {
        public int CacheID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string NormalizedQuestion { get; set; } = string.Empty;
        public string GeneratedSQL { get; set; } = string.Empty;
        public string SQLTemplate { get; set; } = string.Empty;
        public string QuestionVector { get; set; } = string.Empty; // JSON string from DB
        public string NormalizedVector { get; set; } = string.Empty; // JSON string from DB
    }
}
