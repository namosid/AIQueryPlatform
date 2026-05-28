using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class OutputRule
    {
        public string EntityName { get; set; }

        public List<string> MandatoryColumns { get; set; } = new();

        public List<string> OptionalColumns { get; set; } = new();
        public List<string> RequiredEntities { get; set; } = new();

        public string Instruction { get; set; }

    }
}
