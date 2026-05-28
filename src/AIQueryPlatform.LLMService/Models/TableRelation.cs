using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class TableRelation
    {
        public string FromTable { get; set; }

        public string FromColumn { get; set; }

        public string ToTable { get; set; }

        public string ToColumn { get; set; }
    }
}
