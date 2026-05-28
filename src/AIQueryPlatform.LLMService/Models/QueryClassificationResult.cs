using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class QueryClassificationResult
    {
        public ClassificationQueryType QueryType { get; set; }

        public double Confidence { get; set; }

        public List<string> Entities { get; set; }

        public bool RequiresWindowFunctions { get; set; }

        public bool RequiresAggregation { get; set; }

        public bool RequiresTimeSeriesLogic { get; set; }

        public bool RequiresPredictionLogic { get; set; }

        public bool RequiresCTE { get; set; }

        public int ComplexityScore { get; set; }
    }
}
