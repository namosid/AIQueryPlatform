using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.IntentDetector
{
    public class ClarificationAgent
    {
        private readonly ClarificationRegistry _registry;

        public ClarificationAgent(ClarificationRegistry registry)
        {
            _registry = registry;
        }

        public ClarificationQuestion GenerateQuestion(VaguenessResult vagueness)
        {
            // ← options built from REAL schema
            var options = _registry.BuildOptions(vagueness.Entity);

            var sb = new StringBuilder();
            sb.AppendLine($"What {vagueness.Entity} information do you need?\n");

            foreach (var opt in options)
                sb.AppendLine($"  [{opt.Key}] {opt.Label,-20} — {opt.Detail}");

            sb.AppendLine("\nEnter number(s) separated by comma (e.g. 1,3):");

            return new ClarificationQuestion
            {
                Question = sb.ToString(),
                Options = options
            };
        }
    }
    public class ClarificationQuestion
    {
        public string Question { get; set; } = string.Empty;
        public List<ClarificationOption> Options { get; set; } = new();
    }
}
