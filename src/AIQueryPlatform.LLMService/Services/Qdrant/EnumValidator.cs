using AIQueryPlatform.LLMServiceOperator.Models.Qdrant;
using AIQueryPlatform.LLMServiceOperator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AIQueryPlatform.LLMServiceOperator.Services.STM.EntityNameExtractor;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public static class EnumValidator
    {
        private static Dictionary<string, EntityTableMapping> _mappings = new();

        // ── Initialize once at startup ────────────────────────────────────
        public static void Initialize(Dictionary<string, EntityTableMapping> mappings)
        {
            _mappings = mappings;
        }

        // ── Main entry point called from ResolveEntity ────────────────────
        public static EnumValidationResult Validate(EntityType entityType, string rawValue)
        {
            var key = entityType.ToString();  // EntityType.Class → "Class"

            if (!_mappings.TryGetValue(key, out var mapping))
                return EnumValidationResult.PassThrough(rawValue);

            if (!mapping.HasEnumConstraints)
                return EnumValidationResult.PassThrough(rawValue);

            foreach (var column in mapping.ColumnEnumValues.Keys)
            {
                if (mapping.TryResolveEnumValue(column, rawValue, out var resolved))
                    return EnumValidationResult.Valid(column, resolved);

                var normalized = Normalize(entityType, rawValue);
                if (normalized != null &&
                    mapping.TryResolveEnumValue(column, normalized, out var resolvedNorm))
                    return EnumValidationResult.Valid(column, resolvedNorm);
            }

            Helper.LogMessage(string.Format(
                "EnumValidator — no match for EntityType='{0}' RawValue='{1}'",
                entityType, rawValue));

            return EnumValidationResult.Invalid(entityType, rawValue);
        }

        // ── Validate all entities in one pass ─────────────────────────────
        public static List<EnumValidationResult> ValidateAll(
        List<(EntityType EntityType, string RawValue)> rawEntities)
        {
            return rawEntities
                .Select(e => Validate(e.EntityType, e.RawValue))
                .ToList();
        }

        // ── Normalization per EntityType ──────────────────────────────────
        private static string? Normalize(EntityType entityType, string rawValue) =>
        entityType switch
        {
            EntityType.Class => NormalizeClass(rawValue),
            EntityType.Section => NormalizeSection(rawValue),
            _ => null
        };

        private static string? NormalizeClass(string raw)
        {
            // "9"/"9th"/"class9"/"Grade9" → "Grade 9"
            var match = Regex.Match(raw.Trim(), @"\b(\d{1,2})\b");
            if (!match.Success) return null;

            var num = int.Parse(match.Value);
            return (num >= 1 && num <= 12) ? $"Grade {num}" : null;
        }

        private static string? NormalizeSection(string raw)
        {
            // "sec-a" / "section_b" / "a" → "A"
            var match = Regex.Match(raw.Trim(), @"[A-Za-z]$");
            return match.Success ? match.Value.ToUpper() : null;
        }
    }
}
