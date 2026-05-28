using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class QueryParameter
    {
        public string Placeholder { get; set; }  // @AttendanceThreshold
        public object Value { get; set; }  // 75
        public string Type { get; set; }  // "percentage", "name", "date"
    }
}
