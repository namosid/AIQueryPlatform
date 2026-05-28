using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    // ─── Cache Entry — loaded from DB ─────────────────────────
    public class CacheEntry
    {
        public int CacheID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string NormalizedQuestion { get; set; } = string.Empty;
        public string GeneratedSQL { get; set; } = string.Empty;
        public string SQLTemplate { get; set; } = string.Empty;
        public float[] QuestionVector { get; set; } = Array.Empty<float>();
        public float[] NormalizedVector { get; set; } = Array.Empty<float>();
    }
}
