using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class QueryIntent
    {
        public List<ClassificationQueryType> Types { get; set; } = new();

        public List<string> BusinessDomains { get; set; } = new();

        public List<string> Entities { get; set; } = new();

        public int ComplexityScore { get; set; }

        public bool RequiresCTE { get; set; }

        public bool RequiresWindowFunctions { get; set; }

        public bool RequiresAggregation { get; set; }

        public bool RequiresPredictionLogic { get; set; }

        public bool RequiresTimeSeriesLogic { get; set; }

        public bool RequiresOptimization { get; set; }

        public bool RequiresVectorSearch { get; set; }
    }
}
