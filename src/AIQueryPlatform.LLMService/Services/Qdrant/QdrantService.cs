using AIQueryPlatform.LLMServiceOperator.Interface;
using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Models.Qdrant;
using AIQueryPlatform.LLMServiceOperator.Tools;
using Grpc.Net.Client.Balancer;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.IO;
using System.Text;
using System.Text.Json;


namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public class QdrantService
    {
        private readonly QdrantClient _client;
        private const int VectorSize = 1536;
        private readonly string CollectionName;
        public readonly List<(string tableName, string content)> _schemaChunks;
        EntityTableMappingService _mappingService;
        public Dictionary<string, HashSet<string>> _schemaColumns;
        public QdrantService(string fullSchema, string qdrantURL, string qdrantAPIKey, string mappingPath, string collName)
        {
            _client = new QdrantClient(
                host: qdrantURL,
                port: 6334,
                https: true,
                apiKey: qdrantAPIKey
                );
            _schemaChunks = fullSchema
            .Split("Table:")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x =>
            {
                var content = "Table:" + x.Trim();
                var tableName = x.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";
                return (tableName, content);
            }).ToList();
            _schemaColumns = BuildSchemaColumns();
            _mappingService = new EntityTableMappingService(mappingPath, _schemaColumns);
            CollectionName = collName;

        }

        public async Task InitAsync(string filePath, EmbeddingService embeddingService)
        {
            await IndexAsync(filePath, embeddingService);
            //var collections = await _client.ListCollectionsAsync();

            //bool exists = collections.Any(c => c == CollectionName);
            //if (!exists)
            //{
            //    await _client.CreateCollectionAsync(
            //        collectionName: CollectionName,
            //        vectorsConfig: new VectorParams
            //        {
            //            Size = 1536,
            //            Distance = Distance.Cosine
            //        }
            //    );
            //    Console.WriteLine("✅ Qdrant Collection Created");
            //    var schemaChunks = fullSchema
            //        .Split("Table:")
            //        .Where(x => !string.IsNullOrWhiteSpace(x))
            //        .Select(x =>
            //        {
            //            var content = "Table:" + x.Trim();
            //            var tableName = x.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";
            //            return (tableName, content);
            //        })
            //        .ToList();
        }

        public async Task InsertSchemaAsync(string jsonFilePath, EmbeddingService embeddingService)
        {
            //var points = new List<PointStruct>();

            //foreach (var chunk in schemaChunks)
            //{
            //    var vector = await embeddingService.GetEmbeddingAsync(chunk.Content);

            //    points.Add(new PointStruct
            //    {
            //        Id = new PointId { Uuid = Guid.NewGuid().ToString() },
            //        Vectors = vector,
            //        Payload =
            //            {
            //                ["table"] = chunk.Table,
            //                ["content"] = chunk.Content
            //            }
            //    });
            //}

            //await _client.UpsertAsync(CollectionName, points);

            await IndexAsync(jsonFilePath, embeddingService);

            Console.WriteLine("✅ Schema inserted into Qdrant");
        }

        public async Task<SearchOutput> SearchAsync(string question, EmbeddingService embeddingService)
        {
            var queryVector = await embeddingService.GetEmbeddingAsync(question);

            var results = await _client.SearchAsync(
             collectionName: CollectionName,
             vector: queryVector,
             limit: 5,
             scoreThreshold: 0.5f
         );
            // Step 3: Extract table names + content from payload
            var qdrantTables = results
             .Where(r => r.Payload.ContainsKey("primary_table"))
             .Select(r => (
                 tableName: r.Payload["primary_table"].StringValue,
                 content: r.Payload["content"].StringValue,
                 score: r.Score
             ))
             .OrderByDescending(r => r.score)
             .ToList();

            //// Step 3b: Elbow cut — drop long tail below top score
            //if (qdrantTables.Count > 0)
            //{
            //    float topScore = qdrantTables[0].score;
            //    float cutoff = Math.Max(topScore - 0.12f, 0.55f); // whichever is higher
            //    qdrantTables = qdrantTables.Where(r => r.score >= cutoff).ToList();
            //}

            // Step 4: Detect entities from user query
            var detected = EntityDetector.DetectEntities(question);
            var requiredMappingTables = _mappingService.GetRequiredTables(detected);

            var requiredTableNames = new HashSet<string>(
                qdrantTables.Select(t => t.tableName));

            //// Step 5: Get required table names from Output Rules
            //Console.WriteLine("Table Identifing base on Output Rules");
            //var outputTables = BuildOutputRulesPrompt(requiredTableNames);

            //outputTables.UnionWith(requiredMappingTables);
            // Step 6: For injected tables missing from Qdrant results
            var finalSchemaBlocks = GetMissingTables(requiredMappingTables, qdrantTables);
            requiredMappingTables.UnionWith(requiredTableNames);

            SearchOutput output = new SearchOutput()
            {
                Schema = string.Join("\n\n", finalSchemaBlocks),
                QueryVector = queryVector,
                Entities = requiredMappingTables,
                MappingService = _mappingService
            };

            // Step 8: Combine schema + output rules → return to LLM
            return output;

        }

        //pull content directly from schemaChunks(already parsed at startup)
        private List<string> GetMissingTables(HashSet<string> requiredTableNames, List<(string tableName, string content, float score)> qdrantTables)
        {
            var finalSchemaBlocks = new List<string>();

            foreach (var tableName in requiredTableNames)
            {
                // Already retrieved from Qdrant?
                var fromQdrant = qdrantTables.FirstOrDefault(t =>
                    string.Equals(t.tableName, tableName, StringComparison.OrdinalIgnoreCase));

                if (fromQdrant != default)
                {
                    finalSchemaBlocks.Add(fromQdrant.content); // ✅ from Qdrant
                }
                else
                {
                    // Fallback → pull from in-memory schemaChunks
                    var fromChunks = _schemaChunks.FirstOrDefault(c =>
                        string.Equals(c.tableName, tableName, StringComparison.OrdinalIgnoreCase));

                    if (fromChunks != default)
                    {
                        finalSchemaBlocks.Add(fromChunks.content); // ✅ injected from file
                        //Console.WriteLine($"[Injected from file] {tableName}");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] {tableName} not found anywhere!");
                    }
                }
            }
            return finalSchemaBlocks;
        }
        private string CleanSchemaFromQdrant(IEnumerable<ScoredPoint> qdrantResults)
        {
            var sb = new StringBuilder();
            var addedTables = new HashSet<string>();
            foreach (var point in qdrantResults)
            {
                // Extract stringValue from Qdrant payload
                if (point.Payload.TryGetValue("table", out var tableValue) && point.Payload.TryGetValue("content", out var value))
                {
                    string tableName = tableValue.StringValue;

                    // ✅ Skip if already added
                    if (addedTables.Contains(tableName))
                        continue;

                    addedTables.Add(tableName);

                    string tableText = value.StringValue
                        .Replace("\r\n", "\n")
                        .Replace("\\r\\n", "\n")
                        .Trim();

                    sb.AppendLine($"-- Table: {tableName}");
                    sb.AppendLine(tableText);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private HashSet<string> BuildOutputRulesPrompt(HashSet<string> entities)
        {
            var sb = new StringBuilder();
            var requiredTables = new HashSet<string>();
            var outputRules = new OutputRuleEngine()._rules;
            foreach (var entity in entities)
            {
                // Find matching output rule
                var rule = outputRules.FirstOrDefault(r =>
                string.Equals(r.EntityName, entity, StringComparison.OrdinalIgnoreCase)
                );

                if (rule == null)
                {
                    requiredTables.Add(entity);
                    continue;
                }

                // 1. Find which schema chunks contain the mandatory columns
                foreach (var chunk in _schemaChunks)
                {
                    bool chunkHasMandatoryColumn = rule.MandatoryColumns.Any(col =>
                        chunk.content.Contains($"- {col} ", StringComparison.OrdinalIgnoreCase));

                    // Level 2: chunk must OWN the primary key (PK line in schema)
                    bool ownsPrimaryKey = true;


                    if (chunkHasMandatoryColumn & ownsPrimaryKey)
                    {
                        requiredTables.Add(chunk.tableName);
                        //Console.WriteLine($"[OutputRules] '{chunk.tableName}' required for column match");
                    }
                }
            }

            return requiredTables;
        }


        public async Task IndexAsync(string jsonFilePath, EmbeddingService embeddingService)
        {
            if (!await ShouldIndexAsync())
                return;

            // Step 1: Read JSON
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var config = JsonSerializer.Deserialize<EntityMappingConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config?.EntityTableMappings == null || !config.EntityTableMappings.Any())
            {
                Helper.LogMessage(String.Format("No mappings found in {0}", jsonFilePath));
                return;
            }

            // Step 2: Ensure collection exists
            await EnsureCollectionAsync();

            // Step 3: Build + upsert points
            var points = new List<PointStruct>();
            uint id = 1;

            foreach (var mapping in config.EntityTableMappings)
            {
                var document = BuildMappingDocument(mapping);
                var vector = await embeddingService.GetEmbeddingAsync(document);

                var point = new PointStruct
                {
                    Id = new PointId { Num = id++ },
                    Vectors = new Vectors { Vector = new Vector { Data = { vector } } },
                    Payload =
                {
                    ["entity_type"]    = mapping.EntityType,
                    ["primary_table"]  = mapping.PrimaryTable,
                    ["synonyms"] =     new Value
                    {
                        ListValue = new ListValue
                        {
                            Values =
                            {
                                mapping.Synonyms
                                    .Select(c => new Value { StringValue = c })
                            }
                        }
                    },
                    ["content"]        = document,
                    ["join_tables"]    = new Value
                    {
                        ListValue = new ListValue
                        {
                            Values =
                            {
                                mapping.JoinTables
                                    .Select(j => new Value { StringValue = j.Table })
                            }
                        }
                    },
                    ["identifier_columns"] = new Value
                    {
                        ListValue = new ListValue
                        {
                            Values =
                            {
                                mapping.IdentifierColumns
                                    .Select(c => new Value { StringValue = c })
                            }
                        }
                    },
                    ["display_columns"] = new Value
                    {
                        ListValue = new ListValue
                        {
                            Values =
                            {
                                mapping.DisplayColumns
                                    .Select(c => new Value { StringValue = c })
                            }
                        }
                    }
                }
                };

                points.Add(point);
                Helper.LogMessage(String.Format("Prepared point for entity: {0} → {1}",
                    mapping.EntityType, mapping.PrimaryTable));
            }

            // Step 4: Upsert in batches
            const int batchSize = 20;
            foreach (var batch in points.Chunk(batchSize))
            {
                await _client.UpsertAsync(CollectionName, batch);
                Helper.LogMessage(String.Format("Upserted batch of {0} points", batch.Length));
            }

            Helper.LogMessage(String.Format("Indexing complete. Total points: {0}", points.Count));
        }

        private async Task EnsureCollectionAsync()
        {
            var collections = await _client.ListCollectionsAsync();
            if (collections.Any(c => c == CollectionName))
            {
                Helper.LogMessage(String.Format("Collection '{0}' already exists, skipping creation.", CollectionName));
                return;
            }

            await _client.CreateCollectionAsync(CollectionName, new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine
            });

            Helper.LogMessage(String.Format("Created collection '{0}'", CollectionName));
        }

        private string BuildMappingDocument(EntityTableMapping mapping)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Entity: {mapping.EntityType}");
            sb.AppendLine($"Primary Table: {mapping.PrimaryTable}");

            if (mapping.IdentifierColumns.Any())
                sb.AppendLine($"Identifier Columns: {string.Join(", ", mapping.IdentifierColumns)}");

            // Synonyms make "Teacher", "Peon" searchable
            if (mapping.Synonyms.Any())
                sb.AppendLine($"Also known as: {string.Join(", ", mapping.Synonyms)}");


            if (mapping.DisplayColumns.Any())
                sb.AppendLine($"Display Columns: {string.Join(", ", mapping.DisplayColumns)}");

            if (mapping.JoinTables.Any())
            {
                sb.AppendLine("Join Tables:");
                foreach (var join in mapping.JoinTables)
                {
                    var refKey = string.IsNullOrEmpty(join.ReferenceKey) ? "" : $" → {join.ReferenceKey}";
                    sb.AppendLine($"  - {join.Table} via {join.ForeignKey}{refKey}");
                }
            }

            if (mapping.ColumnEnumValues.Any())
            {
                sb.AppendLine("Enum Values:");
                foreach (var kv in mapping.ColumnEnumValues)
                    sb.AppendLine($"  - {kv.Key}: {string.Join(", ", kv.Value)}");
            }

            return sb.ToString().Trim();
        }

        private async Task<bool> ShouldIndexAsync()
        {
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Any(c => c == CollectionName))
            {
                // Collection doesn't exist, create it
                await _client.CreateCollectionAsync(CollectionName, new VectorParams
                {
                    Size = VectorSize,
                    Distance = Distance.Cosine
                });
                return true;
            }

            // Collection exists — check if it has any points
            var info = await _client.GetCollectionInfoAsync(CollectionName);
            if (info.PointsCount == 0)
            {
                Helper.LogMessage("Collection exists but is empty, re-indexing...");
                return true;
            }

            Helper.LogMessage(String.Format("Collection '{0}' already has {1} points, skipping indexing.", CollectionName, info.PointsCount));
            return false;
        }

        private Dictionary<string, HashSet<string>> BuildSchemaColumns()
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (tableName, content) in _schemaChunks)
            {
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in content.Split('\n'))
                {
                    var trimmed = line.Trim();

                    // Lines look like: "- ColumnName (type) NOT NULL"
                    if (!trimmed.StartsWith("- "))
                        continue;

                    var colName = trimmed
                        .TrimStart('-')
                        .Trim()
                        .Split(' ')[0]  // take first token before (type)
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(colName))
                        columns.Add(colName);
                }

                result[tableName] = columns;
            }

            return result;
        }
    }

}
