using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
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
        "staff",
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

        private static readonly Dictionary<string, ClassificationQueryType> EntityToClassification = new()
        {
            // ── Academic ────────────────────────────────────────────────────
            { "Exam",               ClassificationQueryType.AcademicAnalysis   },
            { "ExamResult",         ClassificationQueryType.AcademicAnalysis   },
            { "ReportCard",         ClassificationQueryType.AcademicAnalysis   },
            { "Subject",            ClassificationQueryType.AcademicAnalysis   },

            // ── Attendance ──────────────────────────────────────────────────
            { "Attendance",         ClassificationQueryType.AttendanceAnalysis },

            // ── Financial ───────────────────────────────────────────────────
            { "Fee",                ClassificationQueryType.FinancialAnalysis  },
            { "FeePayment",         ClassificationQueryType.FinancialAnalysis  },
            { "StaffSalary",        ClassificationQueryType.FinancialAnalysis  },
            { "Scholarship",        ClassificationQueryType.FinancialAnalysis  },

            // ── Transactional (people, scope, structure) ────────────────────
            { "Student",            ClassificationQueryType.Transactional      },
            { "Teacher",            ClassificationQueryType.Transactional      },
            { "Staff",              ClassificationQueryType.Transactional      },
            { "Parent",             ClassificationQueryType.Transactional      },
            { "Class",              ClassificationQueryType.Transactional      },
            { "Section",            ClassificationQueryType.Transactional      },
            { "Department",         ClassificationQueryType.Transactional      },
            { "AcademicYear",       ClassificationQueryType.Transactional      },

            // ── Not yet in schema — map to Transactional as placeholder ─────
            { "Hostel",             ClassificationQueryType.Transactional      },
            { "HostelRoom",         ClassificationQueryType.Transactional      },
            { "HostelAllocation",   ClassificationQueryType.Transactional      },
            { "Transport",          ClassificationQueryType.Transactional      },
            { "Vehicle",            ClassificationQueryType.Transactional      },
            { "Timetable",          ClassificationQueryType.Transactional      },
            { "Homework",           ClassificationQueryType.Transactional      },
            { "HomeworkSubmission", ClassificationQueryType.Transactional      },
            { "Library",            ClassificationQueryType.Transactional      },
            { "BookIssue",          ClassificationQueryType.Transactional      },
            { "Asset",              ClassificationQueryType.Transactional      },
            { "Event",              ClassificationQueryType.Transactional      },
            { "Announcement",       ClassificationQueryType.Transactional      },
        };

        public List<ClassificationQueryType> Detect(string question)
        {
            var q = question.ToLower();
            var types = new HashSet<ClassificationQueryType>();

            // ── Step 1: Entity-driven classification via EntityDetector.Rules ────
            var detectedEntities = EntityDetector.Rules
                .Where(r => r.Pattern.IsMatch(question))
                .Select(r => r.EntityType)
                .Distinct()
                .ToList();

            foreach (var entity in detectedEntities)
            {
                if (EntityToClassification.TryGetValue(entity, out var classType))
                    types.Add(classType);
            }

            // ── Step 2: Intent-driven classification (no entity equivalent) ──────
            bool HasWord(string word) =>
                Regex.IsMatch(q, $@"\b{Regex.Escape(word)}\b");

            // Layer 3: Broad domain keyword list — catches anything not yet in JSON
            string[] domainKeywords = {
                "student", "attendance", "exam", "fee", "teacher", "staff",
                "subject", "class", "section", "payment", "result", "scholarship",
                "transport", "library", "hostel", "sports", "activity", "event",
                "grade", "performance", "vehicle", "route", "driver", "guardian",
                "department", "course", "asset", "inventory"
            };

            int layer1 = detectedEntities.Count;
            int layer2 = domainKeywords.Count(e => HasWord(e));
            int entityCount = Math.Max(layer1, layer2);

            if (entityCount >= 3)
                types.Add(ClassificationQueryType.MultiEntityAnalysis);

            // Aggregation
            if (HasWord("total") || HasWord("count") || HasWord("sum") ||
                HasWord("average") || HasWord("avg") ||
                Regex.IsMatch(q, @"\b(what|how much|how many).*(highest|lowest)\b"))
                types.Add(ClassificationQueryType.Aggregation);

            // Ranking
            if (HasWord("rank") ||
                Regex.IsMatch(q, @"\btop\s+\d+\b") ||
                Regex.IsMatch(q, @"\b(who|which).*(highest|lowest)\b"))
                types.Add(ClassificationQueryType.Ranking);

            // Trend
            if (HasWord("trend") || HasWord("growth") || HasWord("increase") ||
                HasWord("decrease") || HasWord("decline") || HasWord("dropped") ||
                HasWord("improved"))
                types.Add(ClassificationQueryType.TrendAnalysis);

            // TimeSeries
            if (HasWord("monthly") || HasWord("yearly") || HasWord("daily") ||
                HasWord("weekly") || HasWord("quarterly") || HasWord("historical") ||
                q.Contains("over time") ||
                Regex.IsMatch(q, @"\b(last|past|previous)\s+(week|month|year|quarter|\d+)\b"))
                types.Add(ClassificationQueryType.TimeSeries);

            // Dashboard
            if (HasWord("dashboard") || HasWord("summary") ||
                HasWord("overview") || HasWord("report"))
                types.Add(ClassificationQueryType.Dashboard);

            // Comparative
            if (HasWord("compare") || HasWord("versus") || HasWord("better") || HasWord("worse"))
                types.Add(ClassificationQueryType.ComparativeAnalysis);

            // DrillDown
            if (HasWord("details") || HasWord("breakdown"))
                types.Add(ClassificationQueryType.DrillDown);

            // Statistical
            if (HasWord("median") || HasWord("variance") || HasWord("distribution"))
                types.Add(ClassificationQueryType.Statistical);

            // Exception
            if (HasWord("anomaly") || HasWord("abnormal") || HasWord("exception"))
                types.Add(ClassificationQueryType.ExceptionDetection);

            // Predictive
            if (HasWord("predict") || HasWord("forecast") || HasWord("likely") || HasWord("risk"))
                types.Add(ClassificationQueryType.PredictiveAnalysis);

            // ── Step 4: Transactional fallback ───────────────────────────────────
            if (types.Count == 0)
                types.Add(ClassificationQueryType.Transactional);

            return types.Distinct().ToList();
        }
    }
}
