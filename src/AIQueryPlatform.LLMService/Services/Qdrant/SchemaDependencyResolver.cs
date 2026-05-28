using AIQueryPlatform.LLMServiceOperator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    // SchemaDependencyResolver.cs
    public class SchemaDependencyResolver
    {
        private readonly Dictionary<string, TableSchema> _fullSchemaLookup;

        public SchemaDependencyResolver()
        {

        }
        public SchemaDependencyResolver(List<TableSchema> fullSchema)
        {
            // Index the FULL schema by table name for O(1) lookup
            _fullSchemaLookup = fullSchema.ToDictionary(t => t.TableName, t => t);
        }

        /// <summary>
        /// Given Qdrant-filtered tables + detected entities,
        /// inject any missing required tables and return enriched schema.
        /// </summary>
        public List<TableSchema> Resolve(
            List<TableSchema> qdrantTables,
            HashSet<string> detectedEntities)
        {
            var result = new Dictionary<string, TableSchema>(
                qdrantTables.ToDictionary(t => t.TableName, t => t));

            foreach (var entity in detectedEntities)
            {
                if (!OutputRulesRegistry.RequiredTables.TryGetValue(entity, out var required))
                    continue;

                foreach (var tableName in required)
                {
                    if (!result.ContainsKey(tableName))
                    {
                        if (_fullSchemaLookup.TryGetValue(tableName, out var tableSchema))
                        {
                            result[tableName] = tableSchema;
                            Console.WriteLine($"[DependencyResolver] Injected missing table: {tableName}");
                        }
                        else
                        {
                            Console.WriteLine($"[DependencyResolver] WARNING: {tableName} not found in full schema!");
                        }
                    }
                }
            }

            return result.Values.ToList();
        }

        public List<string> Resolve(
       List<string> qdrantTableNames,      // what Qdrant returned
       HashSet<string> detectedEntities)   // from EntityDetector
        {
            var result = new HashSet<string>(qdrantTableNames);

            foreach (var entity in detectedEntities)
            {
                if (!OutputRulesRegistry.RequiredTables.TryGetValue(entity, out var required))
                    continue;

                foreach (var tableName in required)
                {
                    if (result.Add(tableName))  // Add returns false if already exists
                        Console.WriteLine($"[Resolver] Injected: {tableName}");
                }
            }

            return result.ToList();
        }
    }
}
