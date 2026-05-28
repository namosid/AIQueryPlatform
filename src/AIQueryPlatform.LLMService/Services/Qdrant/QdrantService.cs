using Grpc.Net.Client.Balancer;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using AIQueryPlatform.LLMServiceOperator.Models;
using System.IO;
using System.Text;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public class QdrantService
    {
        private readonly QdrantClient _client;
        private const string CollectionName = "schema";
        public readonly List<(string tableName, string content)> _schemaChunks;
        public QdrantService(string fullSchema, string qdrantURL, string qdrantAPIKey)
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
        }

        public async Task InitAsync(string fullSchema, EmbeddingService embeddingService)
        {
            var collections = await _client.ListCollectionsAsync();

            bool exists = collections.Any(c => c == CollectionName);
            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    collectionName: CollectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = 1536,
                        Distance = Distance.Cosine
                    }
                );
                Console.WriteLine("✅ Qdrant Collection Created");
                var schemaChunks = fullSchema
                    .Split("Table:")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                    {
                        var content = "Table:" + x.Trim();
                        var tableName = x.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";
                        return (tableName, content);
                    })
                    .ToList();

                await InsertSchemaAsync(schemaChunks, embeddingService);
            }
            else
            {
                Console.WriteLine("✅ Qdrant Collection already exists");
            }

        }

        public async Task InsertSchemaAsync(List<(string Table, string Content)> schemaChunks, EmbeddingService embeddingService)
        {
            var points = new List<PointStruct>();

            foreach (var chunk in schemaChunks)
            {
                var vector = await embeddingService.GetEmbeddingAsync(chunk.Content);

                points.Add(new PointStruct
                {
                    Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                    Vectors = vector,
                    Payload =
                        {
                            ["table"] = chunk.Table,
                            ["content"] = chunk.Content
                        }
                });
            }

            await _client.UpsertAsync(CollectionName, points);

            Console.WriteLine("✅ Schema inserted into Qdrant");
        }

        public async Task<SearchOutput> SearchAsync(string question, EmbeddingService embeddingService)
        {
            var queryVector = await embeddingService.GetEmbeddingAsync(question);

            var results = await _client.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                limit: 5,           // cast a wider net
                scoreThreshold: 0.37f  // only tables with meaningful similarity
            );

            // Step 3: Extract table names + content from payload
            var qdrantTables = results
                .Where(r => r.Payload.ContainsKey("table"))
                .Select(r => (
                    tableName: r.Payload["table"].StringValue,
                    content: r.Payload["content"].StringValue,
                    score: r.Score
                )).ToList();

            // Step 4: Detect entities from user query
            //var entities = EntityDetector.DetectEntities(question);

            var requiredTableNames = new HashSet<string>(
                qdrantTables.Select(t => t.tableName));

            // Step 5: Get required table names from Output Rules
            Console.WriteLine("Table Identifing base on Output Rules");
            var outputTables = BuildOutputRulesPrompt(requiredTableNames);

            // Step 6: For injected tables missing from Qdrant results
            var finalSchemaBlocks = GetMissingTables(outputTables, qdrantTables);

            SearchOutput output = new SearchOutput()
            {
                Schema = string.Join("\n\n", finalSchemaBlocks),
                QueryVector = queryVector,
                Entities = outputTables
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
    }

}
