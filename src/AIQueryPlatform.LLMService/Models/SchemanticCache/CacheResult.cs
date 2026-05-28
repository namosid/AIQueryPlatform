using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    // ─── Cache Result — returned on cache HIT ─────────────────
    public class CacheResult
    {
        public string SQL { get; set; } = string.Empty;
        public string OriginalQuestion { get; set; } = string.Empty;
        public float Similarity { get; set; }
        public bool IsFromCache { get; set; } = true;
    }
}
