using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models.SchemanticCache
{
    public class CacheSearchResult
    {
        public QueryCacheRecord Record { get; set; }
        public float FinalScore { get; set; }
        public MatchType MatchType { get; set; }
    }

    public class ScoredCandidate
    {
        public QueryCacheRecord Record { get; set; }
        public float FinalScore { get; set; }
        public MatchType MatchType { get; set; }
    }

    public enum MatchType
    {
        Hit,        // score above threshold, fields match
        Miss,       // score below threshold
        Blocked     // required fields missing in current session
    }
}
