using AIQueryPlatform.LLMServiceOperator.Interface;
using AIQueryPlatform.LLMServiceOperator.Models;
using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Services;
using AIQueryPlatform.LLMServiceOperator.Services.IntentDetector;
using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
using AIQueryPlatform.LLMServiceOperator.Services.STM;
using AIQueryPlatform.LLMServiceOperator.Tools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator
{
    public class LLMServicePipe : ILLMServicePipe
    {
        private readonly string apiKey;
        private readonly string model;
        private readonly string endPoint;
        private readonly string deploymentName;
        private readonly string apiKeyTextModel;
        private readonly string modelTextModel;
        private readonly string endPointTextModel;
        private readonly string deploymentNameTextModel;
        private readonly string qdrantURL;
        private readonly string qdrantAPIKey;
        private readonly string connectionString;
        private readonly string schoolDBConnectionString;
        private ILlmService llmService;
        private FileSchemaService fileService;


        public LLMServicePipe(IConfiguration configuration, ILlmService lservice)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");

            apiKey = configuration["OpenAI:ApiKey"];
            model = configuration["OpenAI:Model"];
            endPoint = configuration["OpenAI:Endpoint"];
            deploymentName = configuration["OpenAI:DeploymentName"];

            apiKeyTextModel = configuration["TextModelAI:ApiKey"];
            modelTextModel = configuration["TextModelAI:Model"];
            endPointTextModel = configuration["TextModelAI:Endpoint"];
            deploymentNameTextModel = configuration["TextModelAI:DeploymentName"];

            qdrantURL = configuration["QdrantData:URL"];
            qdrantAPIKey = configuration["QdrantData:APIKey"];
            llmService = lservice;
            fileService = new FileSchemaService(configuration);

        }
        public async Task<LLMResponse> ProcessQuery(string userPrompt, TenantData tenant)
        {
            var result = new LLMResponse();
            try
            {
                var fullSchema = fileService.ReadSchema(tenant.SchemaFile);
                if (fullSchema == null)
                {
                    result.Message = "Error: Unable to read database schema.";
                    result.Type = ResponseType.ERROR;
                    return result;
                }
                var embeddingService = new EmbeddingService(apiKeyTextModel, endPointTextModel, deploymentNameTextModel);
                var qdrantService = new QdrantService(fullSchema, qdrantURL, qdrantAPIKey);
                await qdrantService.InitAsync(fullSchema, embeddingService);

                var cacheService = new SemanticCacheService(connectionString, embeddingService);

                // Intent Detection
                var clarificationRegistry = new ClarificationRegistry(qdrantService._schemaChunks);
                var clarificationAgent = new ClarificationAgent(clarificationRegistry);

                // short term memory for follow-up detection and context enrichment
                var memory = new ShortTermMemory(tenant.TenantId);

                if (memory.GetLatestTurn()?.UserInput == userPrompt)
                {
                    result.SQL = memory.GetLatestTurn().GeneratedSQL;
                    result.Type = ResponseType.SQL;
                    return result;
                }
                if (userPrompt.ToLower() == "history")
                {
                    result.Message = memory.GetHistory(HistoryMode.AllChains, includeSQL: false);
                    result.Type = ResponseType.MESSAGE;
                    return result;
                }
                if (userPrompt.ToLower() == "reset")
                {
                    memory.Reset();
                    result.Message = "RESET";
                    result.Type = ResponseType.MESSAGE;
                    return result;
                }

                string finalQuery;
                string originalPrompt = userPrompt;
                string refinedPrompt = userPrompt;
                bool turnAlreadyAdded = false;

                var extractedForIntent = EntityNameExtractor.ResolveEntity(originalPrompt, memory.GetLatestTurn());

                // ── CASE 1: Waiting for clarification answer ───────────
                if (memory.WaitingForAnswer)
                {
                    var (refined, selectedOptions) = memory.ResolveClarification(userPrompt);
                    refinedPrompt = refined;
                    turnAlreadyAdded = true; // ← already added inside ResolveClarification
                }
                // ── CASE 2: Follow-up query ────────────────────────────
                // Check if the new query is a follow-up to the last one (e.g., "What about student10?" after "Show me attendance for student10")
                // "Get info for him"
                else if (memory.IsFollowUp(userPrompt))
                {
                    refinedPrompt = memory.EnrichWithContext(userPrompt);
                    Helper.LogMessage($"[Agent] Follow-up enriched: '{originalPrompt}'");
                }


                // -─ CASE 3: Check Query Intent ─────────────────────────
                else
                {
                    // ── CASE 2b: No entity, no pronoun — continuation query ────────
                    // "include attendance information"
                    // "show marks as well"
                    // "add exam results"
                    if (memory.IsContineousContext(userPrompt, extractedForIntent))
                    {
                        refinedPrompt = memory.BuildRefinedQuery(userPrompt, extractedForIntent);
                    }

                    // ─── Step 1: Analyze intent ────────────────────────────
                    var vagueness = IntentAnalyzer.Analyze(memory.GetHistory() + "\n" + refinedPrompt);

                    if (vagueness.IsVague)
                    {
                        // ─── Step 2: Ask clarification ─────────────────────
                        var clarification = clarificationAgent.GenerateQuestion(vagueness);
                        // Save to memory — remember what was asked
                        memory.SetWaitingForAnswer(userPrompt, vagueness, clarification.Options);

                        Helper.LogMessage($"\nSystem: {clarification.Question}");
                        result.Message = $"\nSystem: {clarification.Question}";
                        result.Type = ResponseType.CLARIFICATION;
                        return result;

                    }
                }

                // PREPARE PROMPT FOR LLM
                finalQuery = BuildLLMPrompt(memory.GetHistory(HistoryMode.CurrentChainOnly, includeSQL: true), refinedPrompt);
                var validate = false;

                var count = 0;
                var IsFailedQuery = false;

                var extractor = EntityNameExtractor.Extract(originalPrompt);
                // ── Step 2: Resolve pronoun from last turn ────────────────────────
                if (extractor.IsPronoun)
                {
                    var lastTurn = memory.GetLatestTurn();
                    if (lastTurn?.ResolvedEntity != null)
                        extractor = lastTurn.ResolvedEntity with { IsPronoun = false };
                }
                var llmPrompt = finalQuery;
                while (!validate)
                {
                    if (count == 3)
                    {
                        Helper.LogMessage("Validation Failed after 3 attempts. Returning last result.");
                        validate = true;
                        IsFailedQuery = true;
                        result.SQL = llmPrompt;
                        result.Type = ResponseType.ERROR;
                        continue;
                    }
                    // ─── LLM Cache Check through Vector ───────────────────────────────────────
                    var cached = await cacheService.GetCacheSearchAsync(refinedQuery: refinedPrompt, normalizedQuery: Normalize(refinedPrompt), memory.GetCurrentChain());
                    if (cached != null)
                    {
                        Helper.LogMessage($"[Cache] HIT ✅ ({cached.FinalScore:P0} similar)");
                        Helper.LogMessage($"SQL:\n{cached.Record.GeneratedSQL}");
                        result.SQL = cached.Record.GeneratedSQL;
                        result.Type = ResponseType.SQL;
                        validate = true;


                        memory.UpdateContext(
                            userInput: originalPrompt,
                            refinedQuery: refinedPrompt,
                            generatedSQL: result.SQL,
                            entity: extractor.Name,
                            entityName: extractor.Name
                        );
                    }
                    else
                    {

                        Helper.LogMessage("No Schemantic Cache Found");
                        Helper.LogMessage("Vector Processing...");
                        var output = await qdrantService.SearchAsync(llmPrompt, embeddingService);
                        Helper.LogMessage("LLM Processing...");
                        result.SQL = await llmService.AskAsync(output.Schema, llmPrompt, memory.GetLatestTurn());

                        // Validaete the result before saving to cache
                        var validator = new DBValidator(fullSchema, tenant.TenantDB);
                        var pipeline = await validator.ValidateQuery(result.SQL, "", memory.GetLatestTurn());

                        Helper.LogMessage("\nLLM Response:");
                        Helper.LogMessage(result.SQL);

                        Helper.LogMessage("Saving Result in Cache");
                        if (pipeline.FinalState == SqlValidator.Models.TurnState.RefinedQuery)
                        {

                            memory.UpdateContext(
                                userInput: originalPrompt,
                                refinedQuery: refinedPrompt,
                                generatedSQL: result.SQL,
                                entity: extractor.Name,
                                entityName: extractor.Name
                            );
                            // Step 4: Save to cache for next time
                            await cacheService.SaveToCacheAsync(
                                memory.GetLatestTurn(),
                                memory.GetCurrentChain(),
                                Normalize(originalPrompt));
                            validate = true;
                            result.Type = ResponseType.SQL;
                        }
                        else
                        {
                            validate = false;
                            if (count == 0)
                            {
                                llmPrompt = BuildLLMPrompt(memory.GetHistory(HistoryMode.CurrentChainOnly, includeSQL: false), refinedPrompt);
                            }
                            llmPrompt += "\n[VALIDATION FEEDBACK]\n" + pipeline.FixHint;
                            llmPrompt += "\n[LAST SQL]\n" + result.SQL + "\n" + "[END SQL]";
                        }
                    }
                    count++;

                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage($"Error: {ex.Message}");
                result.Message = $"Error processing query: {ex.Message}";
                result.Type = ResponseType.CLARIFICATION;
                return result;
            }
            return result;
        }

        private string BuildLLMPrompt(string history, string userPrompt)
        {
            var sb = new StringBuilder();

            // ── Section 1: Conversation History ─────────────────────────
            if (!string.IsNullOrWhiteSpace(history))
            {
                sb.AppendLine("[CONVERSATION HISTORY]");
                sb.AppendLine(history);
                sb.AppendLine("[END HISTORY]");
                sb.AppendLine();
            }

            // ── Section 2: Current Request ───────────────────────────────
            sb.AppendLine("[CURRENT REQUEST]");
            sb.AppendLine(userPrompt);
            sb.AppendLine("[END REQUEST]");

            return sb.ToString();
        }

        private string Normalize(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            // lowercase, remove punctuation, trim extra spaces
            var normalized = query.ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"[^\w\s]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            // Remove filler words
            var stopWords = new[] { "i", "need", "get", "show", "me",
                             "please", "for", "the", "a", "an" };

            normalized = string.Join(" ",
                normalized.Split(' ')
                          .Where(w => !stopWords.Contains(w)));

            return normalized;
            // "need attendance for student10" → "attendance student10"
        }

    }
}
