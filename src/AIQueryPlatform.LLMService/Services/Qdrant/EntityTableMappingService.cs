using AIQueryPlatform.LLMServiceOperator.Models;
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

        public EntityTableMappingService(string jsonPath)
        {
            _mappings = Load(jsonPath);
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
                // entry format → "EntityType:Value"
                var entityType = entry.Contains(':')
                    ? entry.Split(':')[0]
                    : entry;

                if (!results.ContainsKey(entityType) &&
                    _mappings.TryGetValue(entityType, out var mapping))
                    results[entityType] = BuildResult(mapping);
            }

            return results;
        }

        // ── Get all unique tables needed for a question ───────────────────────
        public HashSet<string> GetRequiredTables(HashSet<string> detectedEntities)
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
                AllTables = allTables
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

            foreach (var entry in detectedEntities)
            {
                var entityType = entry.Contains(':') ? entry.Split(':')[0] : entry;
                if (!_mappings.TryGetValue(entityType, out var m)) continue;
                if (!m.ColumnEnumValues.Any()) continue;

                lines.Add($"-- {m.PrimaryTable} column allowed values:");
                foreach (var kv in m.ColumnEnumValues)
                    lines.Add($"--   {kv.Key}: {string.Join(", ", kv.Value.Select(v => $"'{v}'"))}");
            }

            return string.Join("\n", lines);
        }

    }
}
