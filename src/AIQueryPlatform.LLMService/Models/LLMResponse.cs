using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class LLMResponse
    {
        public string SQL { get; set; }
        public ResponseType Type { get; set; }
        public string Message { get; set; }
    }

    public enum ResponseType
    {
        SQL,
        ERROR,
        CLARIFICATION,
        MESSAGE
    }
}
