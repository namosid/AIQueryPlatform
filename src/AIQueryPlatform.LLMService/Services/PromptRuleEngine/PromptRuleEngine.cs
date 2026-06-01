using Qdrant.Client.Grpc;
using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class PromptRuleEngine
    {
        public string BuildRules(string question)
        {
            var detector = new QueryIntentDetector();
            var types = detector.Detect(question);
            return Rules(types);
        }
        private string Rules(List<ClassificationQueryType> types)
        {
            var rules = new List<string>
            {
                // Core
                "- Return valid SQL Server syntax only. No explanation, comments, or markdown.",
                "- Optimize for large datasets.",
                // JOIN
                "- EXISTS/IN: only when joined table has NO SELECT columns. Else JOIN.",
                "- Only JOIN tables needed in SELECT/ORDER BY/GROUP BY.",
                "- LEFT JOIN for optional tables.",
                // Deduplication
                "- Single entity filter (WHERE ID=X): no DISTINCT/GROUP BY.",
                "- All reference tables (no FK): DISTINCT. Any transaction table (has FK): GROUP BY.",
                "- GROUP BY all non-aggregated SELECT columns."
            };

            foreach (var type in types)
            {
                switch (type)
                {
                    case ClassificationQueryType.Aggregation:
                        rules.Add("- Use CTE to pre-aggregate transaction tables before joining.");
                        rules.Add("- Use HAVING instead of WHERE for filtering aggregated results.");
                        rules.Add("- Use aggregate functions efficiently (SUM, COUNT, AVG).");
                        break;

                    case ClassificationQueryType.TrendAnalysis:
                        rules.Add("- Prefer CTEs for multi-step trend logic.");
                        rules.Add("- Use window functions (ROW_NUMBER, LAG, LEAD).");
                        rules.Add("- Avoid correlated subqueries.");
                        break;

                    case ClassificationQueryType.Ranking:
                        rules.Add("- Use ROW_NUMBER() or RANK() with OVER() for ranking.");
                        rules.Add("- Always PARTITION BY relevant grouping column.");
                        break;

                    case ClassificationQueryType.TimeSeries:
                        rules.Add("- Order by date descending for latest records.");
                        rules.Add("- Use BETWEEN or >= / <= for date range filters.");
                        rules.Add("- Exclude rows where date column IS NULL before filtering.");
                        rules.Add("- Cast date columns explicitly when comparing.");
                        break;

                    case ClassificationQueryType.PredictiveAnalysis:
                        rules.Add("- Extract features from aggregated historical data, not raw rows.");
                        break;

                    case ClassificationQueryType.Dashboard:
                        rules.Add("- Each row must represent one unique entity or time period.");
                        rules.Add("- Always include COUNT or SUM for numeric metrics.");
                        rules.Add("- Return chart-friendly grouped summaries.");
                        break;

                    case ClassificationQueryType.ComparativeAnalysis:
                        rules.Add("- Use CASE WHEN for side-by-side comparisons.");
                        rules.Add("- Use clear meaningful aliases for all columns.");
                        break;

                    case ClassificationQueryType.MultiEntityAnalysis:
                        rules.Add("- Use CTE to pre-aggregate transaction tables before JOINing.");
                        break;

                    case ClassificationQueryType.AttendanceAnalysis:
                        rules.Add("- Always GROUP BY StudentID (or StaffID) for attendance queries.");
                        rules.Add("- Attendance% = COUNT(CASE WHEN Status='Present' THEN 1 END) * 100.0 / COUNT(*)");
                        rules.Add("- Use HAVING for attendance % filters (e.g. HAVING AttendancePercent < 75).");
                        rules.Add("- Never return one row per attendance record — always aggregate per student.");
                        rules.Add("- Include TotalDays, PresentDays, AttendancePercent as named columns.");
                        break;
                }
            }

            return string.Join(Environment.NewLine, rules.Distinct());
        }
    }
}
