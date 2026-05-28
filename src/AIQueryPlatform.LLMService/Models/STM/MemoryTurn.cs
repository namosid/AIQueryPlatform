using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIQueryPlatform.LLMServiceOperator.Services.STM.EntityNameExtractor;

namespace AIQueryPlatform.LLMServiceOperator.Models.STM
{
    public class MemoryTurn
    {
        public int TurnNumber { get; set; }
        public string UserInput { get; set; } = string.Empty;
        public string RefinedQuery { get; set; } = string.Empty;
        public string GeneratedSQL { get; set; } = string.Empty;
        public string SystemReply { get; set; } = string.Empty;
        public string? AssistantReply { get; set; }
        public TurnType Type { get; set; }
        public QueryType QueryType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ExtractedEntity? ResolvedEntity { get; set; }
    }
    public class Message
    {
        public string Role { get; set; } = string.Empty;   // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class MemoryChain
    {
        public string ChainID { get; set; } = Guid.NewGuid().ToString();
        public ExtractedEntity Entity { get; set; }
        public List<MemoryTurn> Turns { get; set; } = new();
        public ChainStatus Status { get; set; } = ChainStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinalizedAt { get; set; }
    }

    public enum ChainStatus
    {
        Active,
        Finalized
    }

    public enum HistoryMode
    {
        CurrentChainOnly,   // ← default, most useful for LLM prompt
        AllChains,          // ← for full context or debugging
        RawTurns            // ← your original behavior
    }

    public enum QueryType
    {
        PersonalInfo,    // ← this is the QUERY DOMAIN
        Attendance,
        ExamResult,
        Marks,
        FeePayment,
        ClassReport,
        StaffInfo,
        Unknown
    }
}
