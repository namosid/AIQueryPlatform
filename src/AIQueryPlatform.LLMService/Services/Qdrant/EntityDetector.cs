using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    // EntityDetector.cs
    public static class EntityDetector
    {
        private static readonly Dictionary<string, string> KeywordToEntity = new()
        {
            // Students
            ["student"] = "Students",
            ["students"] = "Students",
            ["attendance"] = "Students",  // ← KEY FIX: attendance queries implicitly need Students
            ["admission"] = "Students",
            ["enroll"] = "Students",
            // Staff
            ["staff"] = "Staff",
            ["teacher"] = "Staff",
            ["employee"] = "Staff",
        };

        public static HashSet<string> DetectEntities(string userQuery)
        {
            var lower = userQuery.ToLower();
            var detected = new HashSet<string>();

            foreach (var kv in KeywordToEntity)
                if (lower.Contains(kv.Key))
                    detected.Add(kv.Value);

            return detected;
        }
    }
}
