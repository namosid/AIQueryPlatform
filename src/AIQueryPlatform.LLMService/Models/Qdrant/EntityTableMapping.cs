using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models.Qdrant
{
    public class EntityTableMapping
    {
        public string EntityType { get; set; } = string.Empty;
        public string PrimaryTable { get; set; } = string.Empty;
        public List<JoinTableInfo> JoinTables { get; set; } = new();
        public List<string> IdentifierColumns { get; set; } = new();
        public List<string> DisplayColumns { get; set; } = new();
        public Dictionary<string, List<string>> ColumnEnumValues { get; set; } = new();
        public List<string> Synonyms { get; set; } = new();
        public string ClassificationType { get; set; } = "Transactional";
        public EntityOutputRule? OutputRule { get; set; }

        // ── Returns true if any enum constraints are defined ──────────────
        public bool HasEnumConstraints => ColumnEnumValues != null && ColumnEnumValues.Any();

        // ── Validates rawValue against enum list, returns DB-cased match ──
        public bool TryResolveEnumValue(string column, string rawValue, out string resolvedValue)
        {
            resolvedValue = rawValue;

            if (!ColumnEnumValues.TryGetValue(column, out var values))
                return true;  // column has no enum = pass through

            var match = values.FirstOrDefault(v =>
                v.Equals(rawValue, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                resolvedValue = match;  // exact DB casing e.g. "Grade 9" not "grade 9"
                return true;
            }

            return false;  // no match found
        }
    }

    public class JoinTableInfo
    {
        public string Table { get; set; } = string.Empty;
        public string JoinOn { get; set; } = string.Empty;
        public string ForeignKey { get; set; } = "";            
        public string? ReferenceKey { get; set; }
    }

    public class EntityMappingLookupResult
    {
        public string PrimaryTable { get; set; } = string.Empty;
        public List<JoinTableInfo> JoinTables { get; set; } = new();
        public List<string> IdentifierColumns { get; set; } = new();
        public List<string> DisplayColumns { get; set; } = new();
        public List<string> AllTables { get; set; } = new(); // primary + joins flattened
        public EntityOutputRule? OutputRule { get; set; }
    }

    public class EntityOutputRule
    {
        public List<string> Always { get; set; } = new();
        public List<string> Optional { get; set; } = new();
        public string? Note { get; set; }
    }
}
