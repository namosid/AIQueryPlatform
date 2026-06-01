using AIQueryPlatform.LLMServiceOperator.Models.Qdrant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    // Models/EntityTableMapping.cs
    public class EntityMappingConfig
    {
        public List<EntityTableMapping> EntityTableMappings { get; set; } = new();
    }
    public class JoinTableConfig
    {
        public string Table { get; set; } = "";
        public string ForeignKey { get; set; } = "";
        public string? ReferenceKey { get; set; }
    }
}
