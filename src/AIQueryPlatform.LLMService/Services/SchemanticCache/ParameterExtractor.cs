using AIQueryPlatform.LLMServiceOperator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class ParameterExtractor
    {
        private static readonly List<(Regex pattern, string type, string placeholder)> Rules = new()
        {
            // Percentages: "below 75%", "less than 60%", "above 80%"
            (new Regex(@"\b(\d+(?:\.\d+)?)\s*%"),          "percentage", "@ThresholdValue"),

            // Named values: "named Student15", "name is John"
            (new Regex(@"\bnamed?\s+['""]?([A-Za-z0-9\s]+)['""]?", RegexOptions.IgnoreCase),
                                                             "name",       "@NameValue"),

            // Dates: "after 2024-01-01", "before 2025-12-31"
            (new Regex(@"\b(\d{4}-\d{2}-\d{2})\b"),         "date",       "@DateValue"),

            // Count/limit: "top 10", "first 5"
            (new Regex(@"\btop\s+(\d+)\b", RegexOptions.IgnoreCase),
                                                             "limit",      "@LimitValue"),

            // Amount: "fees greater than 5000"
            (new Regex(@"\b(\d{4,})\b"),                    "amount",     "@AmountValue"),
        };

        public static (string normalizedQuery, List<QueryParameter> parameters) Extract(string query)
        {
            var normalized = query;
            var parameters = new List<QueryParameter>();

            foreach (var (pattern, type, placeholder) in Rules)
            {
                var match = pattern.Match(normalized);
                if (!match.Success) continue;

                parameters.Add(new QueryParameter
                {
                    Placeholder = placeholder,
                    Value = match.Groups[1].Value,
                    Type = type
                });

                // Replace actual value with placeholder in query
                normalized = pattern.Replace(normalized, placeholder);
            }

            //Console.WriteLine($"[Params] Normalized : '{normalized}'");
            //Console.WriteLine($"[Params] Extracted  : {string.Join(", ", parameters.Select(p => $"{p.Placeholder}={p.Value}"))}");

            return (normalized, parameters);
        }
    }
}
