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
            #region Base Rules
            /*
                -Return ONLY a valid SQL query
                - Do NOT include explanation
                - Do NOT include comments
                - Do NOT include markdown
                - Use ONLY tables and columns from schema
                -Use SQL Server syntax only
                -Prefer CTEs and window functions for trend analysis
                -Use ROW_NUMBER() for latest / previous record comparisons
                - Avoid correlated subqueries when possible
                - Avoid duplicate rows
                - Ensure query is optimized for large datasets
                -Attendance percentage must be calculated as: Present Records / Total Attendance Records * 100
                - For ""last N exams"", use exam date descending order
                - For performance trends: OlderExamMarks > MiddleExamMarks > LatestExamMarks means declining performance
            */
            #endregion
            var rules = new List<string>();
            // Return ONLY valid SQL
            rules.Add("- Return ONLY valid SQL");
            rules.Add("- Do NOT include explanation");
            rules.Add("- Do NOT include comments");
            rules.Add("- Do NOT include markdown");
            rules.Add("- Use SQL Server syntax only");
            rules.Add("- Ensure query is optimized for large datasets");
            // JOIN Rules
            rules.Add("- Use EXISTS or IN ONLY when the joined table has NO columns in the SELECT clause");
            rules.Add("- Use JOIN when any column from the joined table appears in SELECT, ORDER BY, or GROUP BY");
            rules.Add("- Use LEFT JOIN for optional related tables so primary entity always appears");
            rules.Add("- Only JOIN tables whose columns are needed in SELECT, ORDER BY, or GROUP BY");

            //-- Deduplication Rules (apply in order)
            rules.Add("-If query filters to a SINGLE entity (WHERE ID = X) → No DISTINCT or GROUP BY needed");
            rules.Add("- If ALL tables involved are reference/lookup tables (no FK) → Use DISTINCT");
            rules.Add("- If ANY table involved is a transaction table (has FK) → Use GROUP BY");
            //rules.Add("- NEVER use DISTINCT on transaction tables");
            rules.Add("- Always GROUP BY all non-aggregated columns present in SELECT");  // ✅ moved to global


            foreach (var type in types)
            {
                switch (type)
                {
                    case ClassificationQueryType.Aggregation:
                        rules.Add("- Use GROUP BY for aggregations");
                        rules.Add("- Use aggregate functions efficiently");
                        rules.Add("- Use HAVING instead of WHERE for filtering aggregated results");
                        break;

                    case ClassificationQueryType.TrendAnalysis:
                        rules.Add("- ALWAYS use CTEs for readability");
                        rules.Add("- Use window functions (ROW_NUMBER, LAG, LEAD)");
                        rules.Add("- Use ROW_NUMBER() for sequential analysis");
                        rules.Add("- Avoid correlated subqueries");
                        break;

                    case ClassificationQueryType.Ranking:
                        rules.Add("- Use ROW_NUMBER() or RANK() for ranking");
                        rules.Add("- Always partition ranking by relevant grouping column");
                        rules.Add("- Use ORDER BY inside OVER() clause");
                        break;

                    case ClassificationQueryType.TimeSeries:
                        rules.Add("- Order by date descending for latest records");
                        rules.Add("- Use time-series analysis patterns");
                        rules.Add("- Use BETWEEN or >= / <= for date range filters");
                        rules.Add("- Always cast date columns explicitly when comparing");
                        break;


                    case ClassificationQueryType.PredictiveAnalysis:
                        rules.Add("- Generate feature extraction SQL");
                        rules.Add("- Focus on historical patterns");
                        rules.Add("- Use aggregated historical data, not raw rows");
                        break;

                    case ClassificationQueryType.Dashboard:
                        rules.Add("- Return chart-friendly output");
                        rules.Add("- Include grouped summaries");
                        rules.Add("- Each row must represent one unique entity or time period");
                        rules.Add("- Always include COUNT or SUM for numeric metrics");
                        break;

                    case ClassificationQueryType.ComparativeAnalysis:
                        rules.Add("- Use comparison-friendly aggregations");
                        rules.Add("- Use clear meaningful aliases for all columns");
                        rules.Add("- Use CASE WHEN for side-by-side comparisons");
                        break;

                    case ClassificationQueryType.MultiEntityAnalysis:
                        rules.Add("- Optimize JOINs carefully — only JOIN tables needed in SELECT");
                       /// rules.Add("- Use GROUP BY on the primary entity (StudentID, StaffID) when JOINing transaction tables (Attendance, Fees, Examination, ExamResults)");
                        rules.Add("- Use subquery or CTE to pre-aggregate transaction tables before JOINing");
                        break;
                    case ClassificationQueryType.AttendanceAnalysis:
                        rules.Add("- Always GROUP BY StudentID (or StaffID) when querying attendance");
                        rules.Add("- Attendance % = COUNT(CASE WHEN Status='Present' THEN 1 END) * 100.0 / COUNT(*)");
                        rules.Add("- Use HAVING for attendance percentage filters (e.g. HAVING attendance% < 75)");
                        rules.Add("- NEVER return one row per attendance record — always aggregate per student");
                        rules.Add("- Include TotalDays, PresentDays, AttendancePercent as named columns");
                        break;
                }
            }

            return string.Join(Environment.NewLine,
                rules.Distinct());
        }
    }
}
