using AIQueryPlatform.LLMServiceOperator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public static class ParameterExtractor
    {
        // ── Master rule list ────────────────────────────────────────────────
        private static readonly List<(Regex Pattern, string Type, string Placeholder)> Rules = new()
    {
        // ── Numeric ────────────────────────────────────────────────────
        // Percentages: "below 75%", "less than 60.5%", "above 80%"
        (new Regex(@"\b(\d+(?:\.\d+)?)\s*%"),
            "percentage",   "@ThresholdValue"),

        // Large amounts: "fees greater than 5000", "salary 12000"
        (new Regex(@"\b(\d{4,}(?:\.\d+)?)\b"),
            "amount",       "@AmountValue"),

        // Decimal values not caught above: "GPA 3.5", "score 9.2"
        (new Regex(@"\b(\d+\.\d+)\b"),
            "decimal",      "@DecimalValue"),

        // ── Temporal ───────────────────────────────────────────────────
        // ISO dates: "after 2024-01-01", "before 2025-12-31"
        (new Regex(@"\b(\d{4}-\d{2}-\d{2})\b"),
            "date",         "@DateValue"),

        // Month + Year: "in January 2025", "March 2024", "jan 2023"
        (new Regex(@"\b(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|" +
                   @"Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)" +
                   @"\s+(\d{4})\b", RegexOptions.IgnoreCase),
            "monthyear",    "@MonthYearValue"),

        // Standalone year: "in 2023", "for year 2024"
        (new Regex(@"\b(20\d{2}|19\d{2})\b"),
            "year",         "@YearValue"),

        // Day of week: "on Monday", "every Friday"
        (new Regex(@"\b(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)\b",
            RegexOptions.IgnoreCase),
            "dayofweek",    "@DayOfWeekValue"),

        // ── Identity ───────────────────────────────────────────────────
        // Named entity: "named Student15", "name is John Doe"
        (new Regex(@"\bnamed?\s+['""]?([A-Za-z0-9\s]+?)['""]?(?=\s|$)",
            RegexOptions.IgnoreCase),
            "name",         "@NameValue"),

        // Class / section: "class 10A", "grade 5B", "section C"
        (new Regex(@"\b(?:class|grade|section|batch)\s+([A-Za-z0-9\-]+)\b",
            RegexOptions.IgnoreCase),
            "class",        "@ClassValue"),

        // Department: "in Science department", "HR dept"
        (new Regex(@"\b(?:in\s+)?([A-Za-z]+)\s+(?:dept|department)\b",
            RegexOptions.IgnoreCase),
            "department",   "@DepartmentValue"),

        // ── Comparative ────────────────────────────────────────────────
        // Range: "between 60 and 90", "from 2023 to 2025"
        (new Regex(@"\bbetween\s+(\d+(?:\.\d+)?)\s+and\s+(\d+(?:\.\d+)?)\b",
            RegexOptions.IgnoreCase),
            "range",        "@MinValue"),   // captures group 1 → Min, group 2 → Max (handled in extractor)

        // Ratio: "ratio of 3:1", "2:5 split"
        (new Regex(@"\b(\d+)\s*:\s*(\d+)\b"),
            "ratio",        "@RatioValue"),

        // Letter grade: "grade A", "scored B+", "got C-"
        (new Regex(@"\b(?:grade|scored?|got)\s+([A-F][+-]?)\b",
            RegexOptions.IgnoreCase),
            "grade",        "@GradeValue"),

        // ── Ranking / limit ────────────────────────────────────────────
        // Top/bottom/first/last N: "top 10", "bottom 5", "first 3", "last 20"
        (new Regex(@"\b(top|bottom|first|last)\s+(\d+)\b",
            RegexOptions.IgnoreCase),
            "limit",        "@LimitValue"),

        // Pagination: "page 2", "page size 20", "pagesize 50"
        (new Regex(@"\bpage\s*(?:size\s*)?(\d+)\b",
            RegexOptions.IgnoreCase),
            "page",         "@PageValue"),

        // ── Boolean / status ───────────────────────────────────────────
        // Active/inactive/pending/etc.
        (new Regex(@"\b(active|inactive|pending|approved|rejected|enrolled|suspended)\b",
            RegexOptions.IgnoreCase),
            "status",       "@StatusValue"),

        // Gender
        (new Regex(@"\b(male|female)\b",
            RegexOptions.IgnoreCase),
            "gender",       "@GenderValue"),
    };

        // ── Extract all parameters from a query string ──────────────────────
        public static Dictionary<string, string> Extract(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pattern, type, placeholder) in Rules)
            {
                var matches = pattern.Matches(query);
                if (matches.Count == 0) continue;

                // Range rule: two captures → @MinValue + @MaxValue
                if (type == "range" && matches.Count > 0)
                {
                    var m = matches[0];
                    result["@MinValue"] = m.Groups[1].Value;
                    result["@MaxValue"] = m.Groups[2].Value;
                    continue;
                }

                // MonthYear rule: two captures → combine
                if (type == "monthyear" && matches.Count > 0)
                {
                    var m = matches[0];
                    result["@MonthYearValue"] = $"{m.Groups[1].Value} {m.Groups[2].Value}";
                    continue;
                }

                // Multiple matches of same type → suffix with index
                for (int i = 0; i < matches.Count; i++)
                {
                    var key = matches.Count == 1
                        ? placeholder
                        : $"{placeholder}{i}";

                    // Use the last capture group that has a value
                    var value = Enumerable.Range(1, matches[i].Groups.Count - 1)
                        .Select(g => matches[i].Groups[g].Value)
                        .LastOrDefault(v => !string.IsNullOrEmpty(v))
                        ?? matches[i].Value;

                    result[key] = value;
                }
            }

            return result;
        }

        // ── Replace extracted values with placeholders ──────────────────────
        public static string Templatize(string text, Dictionary<string, string> parameters)
        {
            foreach (var kv in parameters.OrderByDescending(k => k.Value.Length))
            {
                // Replace quoted form first: 'Student10' → '@NameValue'
                text = Regex.Replace(text, $"'({Regex.Escape(kv.Value)})'",
                                     $"'{kv.Key}'", RegexOptions.IgnoreCase);

                // Then replace bare form: Student10 → @NameValue
                text = Regex.Replace(text, Regex.Escape(kv.Value),
                                     kv.Key, RegexOptions.IgnoreCase);
            }
            return text;
        }

        // ── Re-inject new values into a template ────────────────────────────
        public static string Apply(
         string template,
         Dictionary<string, string> cachedParams,
         Dictionary<string, string> newParams,
         string? entityName = null)          // ← add this
        {
            var result = template;
            entityName = null;
            // ── 1. Resolve EntityName ────────────────────────────────────
            // Priority: explicit entityName → @NameValue from newParams → @NameValue from cachedParams
            var resolvedEntity = entityName
    ?? newParams.GetValueOrDefault("@NameValue")
    ?? cachedParams.GetValueOrDefault("@NameValue");

            if (!string.IsNullOrEmpty(resolvedEntity))
                result = result.Replace("{EntityName}", resolvedEntity,
                                        StringComparison.OrdinalIgnoreCase);

            // ── 2. Inject @Param placeholders ────────────────────────────
            foreach (var kv in cachedParams)
            {
                var newValue = newParams.TryGetValue(kv.Key, out var v) ? v : kv.Value;
                result = ReplacePlaceholder(result, kv.Key, newValue);
            }

            return result;
        }

        // ── Quote-aware placeholder replacement ─────────────────────────────
        public static string ReplacePlaceholder(string sql, string placeholder, string newValue)
        {
            // Case 1: placeholder is already wrapped in quotes in template
            // e.g. '@NameValue'  or  "@NameValue"  → keep quotes, just swap value
            var singleQuoted = $"'{placeholder}'";
            var doubleQuoted = $"\"{placeholder}\"";

            if (sql.Contains(singleQuoted, StringComparison.OrdinalIgnoreCase))
                return sql.Replace(singleQuoted, $"'{newValue}'",
                                   StringComparison.OrdinalIgnoreCase);

            if (sql.Contains(doubleQuoted, StringComparison.OrdinalIgnoreCase))
                return sql.Replace(doubleQuoted, $"'{newValue}'",
                                   StringComparison.OrdinalIgnoreCase);

            // Case 2: placeholder is bare in template → check if it's a string type
            // that needs quotes (name, status, grade, gender, dayofweek, department)
            var stringTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "@NameValue", "@StatusValue", "@GradeValue",
                "@GenderValue", "@DayOfWeekValue", "@DepartmentValue",
                "@ClassValue", "@MonthYearValue", "@RatioValue"
            };

            var quoted = stringTypes.Contains(placeholder)
                ? $"'{newValue}'"   // wrap in quotes
                : newValue;         // numeric — no quotes

            return sql.Replace(placeholder, quoted, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAggregateOrFilterQuery(string prompt)
        {
            // Has numeric threshold → targeting a group, not one entity
            var hasThreshold = Regex.IsMatch(prompt,
                @"\b\d+(?:\.\d+)?\s*%|\b(?:below|above|less|more|greater|under|over)\s+\d+",
                RegexOptions.IgnoreCase);

            // Has aggregate/list intent
            var hasAggregateIntent = Regex.IsMatch(prompt,
                @"\b(?:all|every|list|find|show|get|which|who|whose)\b",
                RegexOptions.IgnoreCase);

            // Has explicit entity reference — overrides everything
            var hasExplicitEntity = Regex.IsMatch(prompt,
                @"\b(?:named?|for)\s+[A-Za-z0-9]+|\b(?:student|teacher|employee)\s*\d+\b",
                RegexOptions.IgnoreCase);

            return (hasThreshold || hasAggregateIntent) && !hasExplicitEntity;
        }
    }
}
