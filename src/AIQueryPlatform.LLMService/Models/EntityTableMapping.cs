using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class EntityTableMapping
    {
        public string EntityType { get; set; } = string.Empty;
        public string PrimaryTable { get; set; } = string.Empty;
        public List<JoinTableInfo> JoinTables { get; set; } = new();
        public List<string> IdentifierColumns { get; set; } = new();
        public List<string> DisplayColumns { get; set; } = new();
        public Dictionary<string, List<string>> ColumnEnumValues { get; set; } = new();
    }

    public class JoinTableInfo
    {
        public string Table { get; set; } = string.Empty;
        public string JoinOn { get; set; } = string.Empty;
    }

    public class EntityMappingLookupResult
    {
        public string PrimaryTable { get; set; } = string.Empty;
        public List<JoinTableInfo> JoinTables { get; set; } = new();
        public List<string> IdentifierColumns { get; set; } = new();
        public List<string> DisplayColumns { get; set; } = new();
        public List<string> AllTables { get; set; } = new(); // primary + joins flattened
    }
}
