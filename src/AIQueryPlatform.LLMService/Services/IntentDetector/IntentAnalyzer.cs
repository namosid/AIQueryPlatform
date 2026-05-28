using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.IntentDetector
{
    public static class IntentAnalyzer
    {
        // Vague words that need clarification
        private static readonly List<string> VagueTerms = new()
    {
        "details", "information", "info", "data",
        "report", "summary", "everything", "all"
    };

        // Specific terms that need NO clarification
        private static readonly List<string> SpecificTerms = new()
    {
        "attendance", "fee", "mark", "grade",
        "parent", "address", "dob", "birthday",
        "class", "section", "roll","personal", "contact", "enrollment",
        "schedule", "timetable", "performance", "behavior","transport"
    };

        public static VaguenessResult Analyze(string userQuery)
        {
            var lower = userQuery.ToLower();

            var foundVague = VagueTerms
                .Where(t => lower.Contains(t))
                .ToList();

            var foundSpecific = SpecificTerms
                .Where(t => lower.Contains(t))
                .ToList();

            // Vague if has vague terms AND no specific terms
            bool isVague = foundVague.Any() && !foundSpecific.Any();

            return new VaguenessResult
            {
                IsVague = isVague,
                VagueTerms = foundVague,
                SpecificTerms = foundSpecific,
                Entity = DetectEntity(lower)  // "Student", "Staff"
            };
        }

        private static string DetectEntity(string query)
        {
            if (query.Contains("student")) return "Student";
            if (query.Contains("staff") || query.Contains("teacher")) return "Staff";
            if (query.Contains("class")) return "Class";
            return "Unknown";
        }
    }

    public class VaguenessResult
    {
        public bool IsVague { get; set; }
        public List<string> VagueTerms { get; set; } = new();
        public List<string> SpecificTerms { get; set; } = new();
        public string Entity { get; set; } = string.Empty;
    }
}
