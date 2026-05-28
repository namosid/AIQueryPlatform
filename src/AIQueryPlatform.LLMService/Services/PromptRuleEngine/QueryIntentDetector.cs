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
    public class QueryIntentDetector
    {
        public QueryIntent DetectIntent(string question)
        {
            question = question.ToLower();

            var result = new QueryIntent();

            // =====================================================
            // QUERY TYPE DETECTION
            // =====================================================

            DetectQueryTypes(question, result);

            // =====================================================
            // ANALYTICAL REQUIREMENTS
            // =====================================================

            DetectRequirements(question, result);

            // =====================================================
            // COMPLEXITY
            // =====================================================

            result.ComplexityScore = CalculateComplexity(question);

            result.RequiresOptimization =
                result.ComplexityScore >= 3;

            return result;
        }

        private void DetectQueryTypes(string question, QueryIntent result)
        {
            // Trend Analysis

            if (question.Contains("trend") ||
                question.Contains("decline") ||
                question.Contains("increase") ||
                question.Contains("decrease") ||
                question.Contains("last"))
            {
                result.Types.Add(ClassificationQueryType.TrendAnalysis);
            }

            // Aggregation

            if (question.Contains("count") ||
                question.Contains("average") ||
                question.Contains("avg") ||
                question.Contains("sum") ||
                question.Contains("total"))
            {
                result.Types.Add(ClassificationQueryType.Aggregation);

                result.RequiresAggregation = true;
            }

            // Comparison

            if (question.Contains("compare") ||
                question.Contains("better") ||
                question.Contains("worse") ||
                question.Contains("vs"))
            {
                result.Types.Add(ClassificationQueryType.ComparativeAnalysis);
            }

            // Prediction

            if (question.Contains("predict") ||
                question.Contains("likely") ||
                question.Contains("risk") ||
                question.Contains("forecast"))
            {
                result.Types.Add(ClassificationQueryType.PredictiveAnalysis);

                result.RequiresPredictionLogic = true;
            }

            // Attendance

            if (question.Contains("attendance"))
            {
                result.Types.Add(ClassificationQueryType.AttendanceAnalysis);
            }

            // Academic

            if (question.Contains("marks") ||
                question.Contains("exam") ||
                question.Contains("performance"))
            {
                result.Types.Add(ClassificationQueryType.AcademicAnalysis);
            }

            // Multi Entity

            if (result.Entities.Count > 2)
            {
                result.Types.Add(ClassificationQueryType.MultiEntityAnalysis);
            }

            result.Types = result.Types
                .Distinct()
                .ToList();
        }

        private void DetectRequirements(string question, QueryIntent result)
        {
            // Window Functions

            if (Regex.IsMatch(question, @"last\s+\d+") ||
                question.Contains("top") ||
                question.Contains("rank") ||
                question.Contains("highest") ||
                question.Contains("lowest") ||
                question.Contains("continuously"))
            {
                result.RequiresWindowFunctions = true;
            }

            // Time-Series

            if (Regex.IsMatch(question, @"last\s+\d+") ||
                question.Contains("monthly") ||
                question.Contains("yearly") ||
                question.Contains("historical") ||
                question.Contains("continuously"))
            {
                result.RequiresTimeSeriesLogic = true;
            }

            // CTE

            if (result.RequiresTimeSeriesLogic ||
                result.RequiresWindowFunctions ||
                result.RequiresAggregation)
            {
                result.RequiresCTE = true;
            }

            // Vector Search

            if (question.Contains("similar") ||
                question.Contains("related") ||
                question.Contains("semantic"))
            {
                result.RequiresVectorSearch = true;
            }
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

        public List<ClassificationQueryType> Detect(string question)
        {
            question = question.ToLower();

            var types = new List<ClassificationQueryType>();

            // =====================================
            // Transactional
            // =====================================

            if (question.Contains("show") ||
                question.Contains("get") ||
                question.Contains("find"))
            {
                types.Add(ClassificationQueryType.Transactional);
            }

            // =====================================
            // Aggregation
            // =====================================

            if (question.Contains("count") ||
                question.Contains("sum") ||
                question.Contains("average") ||
                question.Contains("avg") ||
                question.Contains("total") ||
                question.Contains("highest") ||
                question.Contains("lowest"))
            {
                types.Add(ClassificationQueryType.Aggregation);
            }

            // =====================================
            // Trend Analysis
            // =====================================

            if (question.Contains("trend") ||
                question.Contains("increase") ||
                question.Contains("decrease") ||
                question.Contains("decline") ||
                question.Contains("declined") ||
                question.Contains("dropped") ||
                question.Contains("improved") ||
                question.Contains("growth") ||
                question.Contains("continuously"))
            {
                types.Add(ClassificationQueryType.TrendAnalysis);
            }

            // =====================================
            // Comparative Analysis
            // =====================================

            if (question.Contains("compare") ||
                question.Contains("vs") ||
                question.Contains("better") ||
                question.Contains("worse"))
            {
                types.Add(ClassificationQueryType.ComparativeAnalysis);
            }

            // =====================================
            // Predictive Analysis
            // =====================================

            if (question.Contains("predict") ||
                question.Contains("forecast") ||
                question.Contains("likely") ||
                question.Contains("risk"))
            {
                types.Add(ClassificationQueryType.PredictiveAnalysis);
            }

            // =====================================
            // Dashboard
            // =====================================

            if (question.Contains("dashboard") ||
                question.Contains("summary") ||
                question.Contains("overview"))
            {
                types.Add(ClassificationQueryType.Dashboard);
            }

            // =====================================
            // Ranking
            // =====================================

            if (question.Contains("top") ||
                question.Contains("rank") ||
                question.Contains("highest") ||
                question.Contains("lowest"))
            {
                types.Add(ClassificationQueryType.Ranking);
            }

            // =====================================
            // TimeSeries
            // =====================================

            if (Regex.IsMatch(question, @"last\s+\d+") ||
                question.Contains("monthly") ||
                question.Contains("yearly") ||
                question.Contains("historical") ||
                question.Contains("over time"))
            {
                types.Add(ClassificationQueryType.TimeSeries);
            }

            // =====================================
            // DrillDown
            // =====================================

            if (question.Contains("details") ||
                question.Contains("breakdown"))
            {
                types.Add(ClassificationQueryType.DrillDown);
            }

            // =====================================
            // Statistical
            // =====================================

            if (question.Contains("median") ||
                question.Contains("variance") ||
                question.Contains("distribution"))
            {
                types.Add(ClassificationQueryType.Statistical);
            }

            // =====================================
            // Exception Detection
            // =====================================

            if (question.Contains("anomaly") ||
                question.Contains("abnormal") ||
                question.Contains("exception"))
            {
                types.Add(ClassificationQueryType.ExceptionDetection);
            }

            // =====================================
            // Correlation Analysis
            // =====================================

            if (question.Contains("correlation") ||
                question.Contains("relationship"))
            {
                types.Add(ClassificationQueryType.CorrelationAnalysis);
            }

            // =====================================
            // Financial Analysis
            // =====================================

            if (question.Contains("fee") ||
                question.Contains("payment") ||
                question.Contains("revenue"))
            {
                types.Add(ClassificationQueryType.FinancialAnalysis);
            }

            // =====================================
            // Academic Analysis
            // =====================================

            if (question.Contains("exam") ||
                question.Contains("marks") ||
                question.Contains("performance"))
            {
                types.Add(ClassificationQueryType.AcademicAnalysis);
            }

            // =====================================
            // Attendance Analysis
            // =====================================

            if (question.Contains("attendance"))
            {
                types.Add(ClassificationQueryType.AttendanceAnalysis);
            }

            // =====================================
            // Multi Entity Analysis
            // =====================================

            int entityCount = 0;

            string[] entities =
            {
            "student",
            "attendance",
            "exam",
            "fee",
            "teacher",
            "staff",
            "subject",
            "class",   
            "section",
            "payment",
            "result",
            "scholarship",
            "transport",
            "library",
            "hostel",
            "sports",
            "activity",
            "event",
            "grade",
            "performance",
            "vehicle",
            "route",
            "driver",
            "guardian",
            "department",
            "course",
            "asset",
            "inventory"

        };

            foreach (var entity in entities)
            {
                if (question.Contains(entity))
                {
                    entityCount++;
                }
            }

            if (entityCount >= 3)
            {
                types.Add(ClassificationQueryType.MultiEntityAnalysis);
            }

            return types.Distinct().ToList();
        }
    }
}
