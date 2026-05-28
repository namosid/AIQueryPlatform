using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class QueryClassification
    {
        public static QueryClassificationResult Detect(string question)
        {
            question = question.ToLower();

            var result = new QueryClassificationResult();

            if (Regex.IsMatch(question, @"last\\s+\\d+"))
            {
                result.RequiresTimeSeriesLogic = true;
                result.RequiresWindowFunctions = true;
                result.RequiresCTE = true;
            }

            if (question.Contains("trend") ||
                question.Contains("decline") ||
                question.Contains("increase"))
            {
                result.QueryType = ClassificationQueryType.TrendAnalysis;
                result.RequiresWindowFunctions = true;
            }

            if (question.Contains("predict") ||
                question.Contains("risk") ||
                question.Contains("likely"))
            {
                result.QueryType = ClassificationQueryType.PredictiveAnalysis;
                result.RequiresPredictionLogic = true;
            }

            if (question.Contains("compare"))
            {
                result.QueryType = ClassificationQueryType.ComparativeAnalysis;
            }

            result.ComplexityScore = CalculateComplexity(question);

            return result;
        }

        private static int CalculateComplexity(string question)
        {
            int score = 0;

            question = question.ToLower();

            // -----------------------------------------
            // JOIN / Multi-Entity Complexity
            // -----------------------------------------

            string[] entities =
            {
        "student",
        "attendance",
        "exam",
        "fee",
        "teacher",
        "subject",
        "payment",
        "class",
        "section",
        "scholarship",
        "transport"
    };

            int entityMatches = entities.Count(e => question.Contains(e));

            score += entityMatches * 5;

            // -----------------------------------------
            // Aggregation Complexity
            // -----------------------------------------

            string[] aggregationWords =
            {
        "count",
        "sum",
        "average",
        "avg",
        "total",
        "highest",
        "lowest",
        "maximum",
        "minimum"
    };

            int aggregationMatches =
                aggregationWords.Count(w => question.Contains(w));

            score += aggregationMatches * 10;

            // -----------------------------------------
            // Trend / Time-Series Complexity
            // -----------------------------------------

            string[] trendWords =
            {
        "trend",
        "increase",
        "decrease",
        "decline",
        "growth",
        "last",
        "previous",
        "monthly",
        "yearly",
        "historical",
        "continuously"
    };

            int trendMatches =
                trendWords.Count(w => question.Contains(w));

            score += trendMatches * 15;

            // -----------------------------------------
            // Prediction Complexity
            // -----------------------------------------

            string[] predictionWords =
            {
        "predict",
        "forecast",
        "risk",
        "likely",
        "probability",
        "future"
    };

            int predictionMatches =
                predictionWords.Count(w => question.Contains(w));

            score += predictionMatches * 20;

            // -----------------------------------------
            // Comparison Complexity
            // -----------------------------------------

            string[] comparisonWords =
            {
        "compare",
        "versus",
        "vs",
        "better",
        "worse"
    };

            int comparisonMatches =
                comparisonWords.Count(w => question.Contains(w));

            score += comparisonMatches * 12;

            // -----------------------------------------
            // Conditional Logic Complexity
            // -----------------------------------------

            string[] conditionalWords =
            {
        "and",
        "or",
        "between",
        "greater than",
        "less than",
        "above",
        "below"
    };

            int conditionMatches =
                conditionalWords.Count(w => question.Contains(w));

            score += conditionMatches * 4;

            // -----------------------------------------
            // Multi-Step Analytical Queries
            // -----------------------------------------

            if (question.Contains("whose"))
                score += 10;

            if (question.Contains("for each"))
                score += 10;

            if (question.Contains("group by"))
                score += 15;

            // -----------------------------------------
            // Time Range Detection
            // -----------------------------------------

            if (Regex.IsMatch(question, @"last\s+\d+"))
                score += 20;

            // -----------------------------------------
            // Normalize Complexity
            // -----------------------------------------

            if (score <= 20)
                return 1; // Simple

            if (score <= 50)
                return 2; // Moderate

            if (score <= 90)
                return 3; // Complex

            return 4; // Very Complex
        }
    }
}
