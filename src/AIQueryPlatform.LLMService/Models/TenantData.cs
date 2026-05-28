using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class TenantData
    {
        public string TenantDB { get; set; }
        public string TenantName { get; set; }
        public string TenantId { get; set; }
        public string SchemaFile { get; set; }
        public string ConversationID { get; set; }

    }
}
