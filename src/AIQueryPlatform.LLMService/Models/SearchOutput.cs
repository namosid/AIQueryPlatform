using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class SearchOutput
    {
        public string Schema { get; set; }
        public float[] QueryVector { get; set; }
        public HashSet<string> Entities { get; set; }
        public EntityTableMappingService MappingService { get; set; }
    }
}
