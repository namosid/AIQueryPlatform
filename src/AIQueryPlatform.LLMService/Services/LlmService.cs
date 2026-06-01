using AIQueryPlatform.LLMServiceOperator.Interface;
using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Tools;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using AIQueryPlatform.LLMServiceOperator.Models.STM;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmOptions _options;

        public LlmService(HttpClient httpClient, IOptions<LlmOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> AskAsync(SearchOutput entityOutput, string prompt, MemoryTurn turn)
        {
            var engine = new PromptRuleEngine();
            var rules = engine.BuildRules(prompt);
            //var outputEngine = new OutputRuleEngine();
            var outputRules = entityOutput.MappingService.BuildOutputRules(entityOutput.Entities);
            var enumContext = entityOutput.MappingService.BuildEnumContext(entityOutput.Entities);
            var fullPrompt = $@"
                You are an expert SQL Server database architect.

                STRICT RULES:
                {rules}
                {outputRules}
                Schema:
                ----------------
                {entityOutput.Schema}
                ----------------
                 Column allowed values (use EXACTLY these values in WHERE clauses):
                {enumContext}
                User Question:
                {prompt}
                ";

            Console.WriteLine("Full Prompt");
            Console.WriteLine(fullPrompt);
            string apiVersion = "2024-02-01";

            string url = $"{_options.Endpoint}/openai/deployments/{_options.Model}/chat/completions?api-version={apiVersion}";

            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                new { role = "user", content = fullPrompt }
            },
                max_tokens = 500,
                temperature = 0.7
            };



            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure OpenAI Error: {response.StatusCode} - {responseContent}");
            }
            Console.WriteLine("LLM Repsonded");
            var finalOutput = ExtractSqlQuery(responseContent);
            return finalOutput;
        }

        public async Task<string> AskAsyncDelay(string schemaText, string userPrompt, string previousSQL)
        {
            try
            {
                var fullPrompt = $@"
            You are an expert SQL Server query generator.

            STRICT RULES:
            - Return ONLY a valid SQL query
            - Do NOT include explanation
            - Do NOT include comments
            - Do NOT include markdown (no ```sql)
            - Use ONLY tables and columns from schema
            - If query cannot be generated, return: INVALID_REQUEST

            Schema:
            {schemaText}

            Question:
            {userPrompt}
            ";

                if (!String.IsNullOrEmpty(previousSQL))
                {
                    fullPrompt += "\n[PreviousSQL]:\n" + previousSQL;
                }

                var requestBody = new
                {
                    model = _options.Model,
                    messages = new[]
                {
            new { role = "user", content = fullPrompt }
        }
                };

                int maxRetries = 3;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                    request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
                    request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"429 hit. Retrying... Attempt {attempt}");

                        if (attempt == maxRetries)
                            throw new Exception("Rate limit exceeded. Try again later.");

                        await Task.Delay(2000 * attempt); // exponential backoff
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }

                throw new Exception("Unexpected error");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "INVALID_REQUEST";
            }
        }

        public string ExtractSql(string response)
        {
            // Remove markdown
            response = response.Replace("```sql", "")
                               .Replace("```", "")
                               .Trim();

            // If extra text exists, try to extract SQL
            var lines = response.Split('\n');

            var sqlLines = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            return string.Join("\n", sqlLines);
        }

        public string ExtractSqlQuery(string response)
        {
            var jsonDoc = JsonDocument.Parse(response);

            string sqlQuery = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return sqlQuery;
        }

        public bool IsSafeQuery(string sql)
        {
            var blocked = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "UPDATE", "INSERT" };

            return !blocked.Any(b => sql.ToUpper().Contains(b));
        }


    }
}
