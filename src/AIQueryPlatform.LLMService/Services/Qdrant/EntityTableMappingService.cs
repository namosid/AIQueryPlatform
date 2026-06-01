using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Models.Qdrant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public class EntityTableMappingService
    {
        private readonly Dictionary<string, EntityTableMapping> _mappings;
        private readonly Dictionary<string, HashSet<string>> _schemaColumns;

        public EntityTableMappingService(string jsonPath, Dictionary<string, HashSet<string>> schemaColumns)
        {
            _mappings = Load(jsonPath);
            _schemaColumns = schemaColumns;
        }

        // ── Load from JSON ───────────────────────────────────────────────────
        private static Dictionary<string, EntityTableMapping> Load(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var root = JsonSerializer.Deserialize<JsonElement>(json);
            var list = root.GetProperty("EntityTableMappings")
                             .Deserialize<List<EntityTableMapping>>()
                         ?? new List<EntityTableMapping>();

            return list.ToDictionary(
                m => m.EntityType,
                m => m,
                StringComparer.OrdinalIgnoreCase);
        }

        // ── Get mapping for a single entity type ─────────────────────────────
        public EntityMappingLookupResult? GetMapping(string entityType)
        {
            if (!_mappings.TryGetValue(entityType, out var mapping))
                return null;

            return BuildResult(mapping);
        }

        // ── Get mappings for all detected entities ───────────────────────────
        // Input: HashSet from EntityDetector.DetectEntities()
        // e.g.  { "Student:Student10", "Class:10A", "Subject:Mathematics" }
        public Dictionary<string, EntityMappingLookupResult> GetMappings(
            HashSet<string> detectedEntities)
        {
            var results = new Dictionary<string, EntityMappingLookupResult>(
                              StringComparer.OrdinalIgnoreCase);

            foreach (var entry in detectedEntities)
            {
                var entityType = entry.Contains(':') ? entry.Split(':')[0] : entry;

                if (results.ContainsKey(entityType)) continue;

                // 1. Direct key lookup
                if (_mappings.TryGetValue(entityType, out var mapping))
                {
                    results[entityType] = BuildResult(mapping);
                    continue;
                }

                // 2. Synonym fallback
                var bysynonym = _mappings.Values.FirstOrDefault(m =>
                    m.Synonyms.Any(s => string.Equals(s, entityType, StringComparison.OrdinalIgnoreCase)));

                if (bysynonym != null)
                    results[entityType] = BuildResult(bysynonym);

                
            }
            
            return results;


        }

        // ── Get all unique tables needed for a question ───────────────────────
        public HashSet<string> GetRequiredJoinTables(HashSet<string> detectedEntities)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mapping in GetMappings(detectedEntities).Values)
            {
                tables.Add(mapping.PrimaryTable);
                foreach (var join in mapping.JoinTables)
                    tables.Add(join.Table);
            }

            return tables;
        }

        public HashSet<string> GetRequiredTables(HashSet<string> detectedEntities)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mappings = GetMappings(detectedEntities).Values;
            foreach (var mapping in mappings)
            {
                // Always include primary table
                tables.Add(mapping.PrimaryTable);

                // Collect columns that actually matter for this entity
                var requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // From OutputRule
                if (mapping.OutputRule != null)
                {
                    foreach (var col in mapping.OutputRule.Always)
                        requiredColumns.Add(col);
                    foreach (var col in mapping.OutputRule.Optional)
                        requiredColumns.Add(col);
                }

                // From IdentifierColumns (needed for WHERE clauses)
                //foreach (var col in mapping.IdentifierColumns)
                //    requiredColumns.Add(col);

                // Only add join tables whose columns appear in requiredColumns
                // Only add join tables whose columns overlap with requiredColumns
                foreach (var join in mapping.JoinTables)
                {
                    var joinEntityMapping = _mappings.Values
                        .FirstOrDefault(m => m.PrimaryTable.Equals(
                            join.Table, StringComparison.OrdinalIgnoreCase));

                   if (_schemaColumns.TryGetValue(join.Table, out var schemaColumns))
                    {
                        if (schemaColumns.Overlaps(requiredColumns))
                            tables.Add(join.Table);
                    }
                }
            }

            return tables;
        }

        // ── Get primary table only ────────────────────────────────────────────
        public string? GetPrimaryTable(string entityType)
            => _mappings.TryGetValue(entityType, out var m) ? m.PrimaryTable : null;

        // ── Build result ──────────────────────────────────────────────────────
        private static EntityMappingLookupResult BuildResult(EntityTableMapping mapping)
        {
            var allTables = new List<string> { mapping.PrimaryTable };
            allTables.AddRange(mapping.JoinTables.Select(j => j.Table));

            return new EntityMappingLookupResult
            {
                PrimaryTable = mapping.PrimaryTable,
                JoinTables = mapping.JoinTables,
                IdentifierColumns = mapping.IdentifierColumns,
                DisplayColumns = mapping.DisplayColumns,
                AllTables = allTables,
                OutputRule = mapping.OutputRule
            };
        }

        // Get enum hint string for a table+column
        public string? GetEnumHint(string entityType, string column)
        {
            if (!_mappings.TryGetValue(entityType, out var m)) return null;
            if (!m.ColumnEnumValues.TryGetValue(column, out var values)) return null;
            return $"{column} allowed values: {string.Join(", ", values.Select(v => $"'{v}'"))}";
        }

        // Get ALL enum hints for a detected entity set — inject into LLM prompt
        public string BuildEnumContext(HashSet<string> detectedEntities)
        {
            var lines = new List<string>();

            foreach (var m in _mappings.Values
                .Where(m => detectedEntities.Contains(m.PrimaryTable, StringComparer.OrdinalIgnoreCase)
                         && m.ColumnEnumValues.Any()))
            {
                lines.Add($"-- {m.PrimaryTable} column allowed values:");
                foreach (var kv in m.ColumnEnumValues)
                    lines.Add($"--   {kv.Key}: {string.Join(", ", kv.Value.Select(v => $"'{v}'"))}");
            }

            return string.Join("\n", lines);
        }

        public string BuildOutputRules(HashSet<string> detectedEntities)
        {
            var lines = _mappings.Values
                .Where(m => m.OutputRule != null &&
                            detectedEntities.Contains(m.PrimaryTable, StringComparer.OrdinalIgnoreCase))
                .Select(m =>
                {
                    var rule = m.OutputRule!;
                    var always = string.Join(",", rule.Always);
                    var optional = rule.Optional.Count > 0
                                   ? $" [opt:{string.Join(",", rule.Optional)}]"
                                   : string.Empty;
                    var note = rule.Note != null ? $" ({rule.Note})" : string.Empty;
                    return $"{m.EntityType}: {always}{optional}{note}";
                })
                .ToList();

            return lines.Count == 0
                ? string.Empty
                : "OUTPUT COLS:\n" + string.Join("\n", lines);
        }

    }
}
