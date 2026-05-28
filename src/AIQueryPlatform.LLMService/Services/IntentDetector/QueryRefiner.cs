using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.IntentDetector
{
    public static class QueryRefiner
    {
        public static string Refine(
            string originalQuery,
            string entity,
            List<string> selectedKeys,
            List<ClarificationOption> options)  // ← pass options with table names
        {
            var selected = options
                .Where(o => selectedKeys.Contains(o.Key))
                .ToList();

            if (!selected.Any()) return originalQuery;

            var details = selected.Select(o => o.Detail);
            var tables = selected.SelectMany(o => o.Tables).Distinct().ToList();

            var refined = $"{originalQuery} — specifically {string.Join(" and ", details)}";

            Console.WriteLine($"[Refiner] Refined query : {refined}");
            Console.WriteLine($"[Refiner] Required tables: {string.Join(", ", tables)}");

            return refined;
        }
    }
}
