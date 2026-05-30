using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Services.IntentDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AIQueryPlatform.LLMServiceOperator.Services.STM.EntityNameExtractor;

namespace AIQueryPlatform.LLMServiceOperator.Services.STM
{
    public class ShortTermMemory
    {
        private SessionContext _session;
        private ExtractedEntity? _lastResolvedEntity;
        

        public ShortTermMemory(string conversationID)
        {
            _session = SessionManager.Instance.GetOrCreate(conversationID);
        }

        // ─── Detect if query is follow-up ─────────────────────
        public bool IsFollowUp(string userInput)
        {
            if (!_session.HasActiveEntity) return false;

            var lower = userInput.ToLower();

            // Follow-up signals
            var followUpSignals = new[]
            {
            "also", "what about", "and his", "and her",
            "show his", "show her", "his ", "her ",
            "same student", "that student", "this student"
            };

            return followUpSignals.Any(s => lower.Contains(s));
        }

        // ─── Enrich follow-up with session context ─────────────
        public string EnrichWithContext(string userInput)
        {
            if (!_session.HasActiveEntity) return userInput;

            // Inject entity name into follow-up query
            var enriched = userInput
           .Replace(" his ", $" {_session.ActiveEntityName}'s ")
           .Replace(" her ", $" {_session.ActiveEntityName}'s ")
           .Replace("what about ", "")
           .Replace("include ", "")
           .Replace("also ", "");

            // Add entity context if not already mentioned
            if (!enriched.Contains(_session.ActiveEntityName!,
                StringComparison.OrdinalIgnoreCase))
            {
                enriched = $"{enriched} for {_session.ActiveEntity} {_session.ActiveEntityName}";
            }

            Console.WriteLine($"[Memory] Enriched: '{userInput}' → '{enriched}'");
            return enriched;
        }

        // ─── Check if info already fetched ────────────────────
        public bool AlreadyFetched(string infoType) =>
            _session.FetchedInfoTypes.Contains(infoType, StringComparer.OrdinalIgnoreCase);

        // ─── Update session after successful query ─────────────
        public void UpdateContext(
            string userInput,
            string refinedQuery,
            string generatedSQL,
            string? entityName = null,
            string? entity = null,
            string? infoType = null)
        {
            // Update active entity
            if (!string.IsNullOrEmpty(entityName))
                _session.ActiveEntityName = entityName;

            if (!string.IsNullOrEmpty(entity))
                _session.ActiveEntity = entity;

            // Track fetched info types
            if (!string.IsNullOrEmpty(infoType) && !AlreadyFetched(infoType))
                _session.FetchedInfoTypes.Add(infoType);


            var current = EntityNameExtractor.Extract(refinedQuery);
            // 1. Resolve pronoun from last known entity
            if (current.IsPronoun && _lastResolvedEntity != null)
            {
                current = _lastResolvedEntity with { IsPronoun = false };
            }

            // 2. Detect context switch
            bool isSwitch = DetectContextSwitch(current);

            if (isSwitch)
            {
                // Save current chain → start new one
                _session.FinalizeCurrentChain();
                _session.StartNewChain(current);
            }
            else
            {
                // Same context → append
                _session.AppendToCurrentChain(refinedQuery, current);
            }

            // 3. Always update last resolved entity (if not pronoun)
            if (!current.IsPronoun)
                _lastResolvedEntity = current;

            var queryType = QueryTypeDetector.Detect(refinedQuery);
            // Add to history
            _session.AddTurn(new MemoryTurn
            {
                UserInput = userInput,
                RefinedQuery = refinedQuery,
                GeneratedSQL = generatedSQL,
                Type = TurnType.RefinedQuery,
                QueryType = queryType,
                ResolvedEntity = current
            });

            Console.WriteLine($"[Memory] Context updated — Entity: {_session.ActiveEntity} '{_session.ActiveEntityName}'");
            Console.WriteLine($"[Memory] Fetched so far: {string.Join(", ", _session.FetchedInfoTypes)}");
        }

        // ─── Save clarification state ──────────────────────────
        public void SetWaitingForAnswer(
            string originalQuery,
            VaguenessResult vagueness,
            List<ClarificationOption> options)
        {
            _session.WaitingForAnswer = true;
            _session.OriginalQuery = originalQuery;
            _session.VaguenessContext = vagueness;
            _session.PendingOptions = options;

            _session.AddTurn(new MemoryTurn
            {
                UserInput = originalQuery,
                SystemReply = "Asked clarification",
                Type = TurnType.ClarificationAsked
            });
        }

        // ─── Resolve clarification answer ─────────────────────
        public (string refinedQuery, List<ClarificationOption> selectedOptions)
            ResolveClarification(string userAnswer)
        {
            var selectedKeys = userAnswer
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            var selectedOptions = _session.PendingOptions
                .Where(o => selectedKeys.Contains(o.Key))
                .ToList();

            var refined = QueryRefiner.Refine(
                _session.OriginalQuery!,
                _session.VaguenessContext!.Entity,
                selectedKeys,
                _session.PendingOptions);

            // Track fetched info types from selection
            foreach (var opt in selectedOptions)
                if (!AlreadyFetched(opt.Label))
                    _session.FetchedInfoTypes.Add(opt.Label);

            // Reset waiting state
            _session.WaitingForAnswer = false;
            _session.OriginalQuery = null;
            _session.VaguenessContext = null;

            _session.AddTurn(new MemoryTurn
            {
                UserInput = userAnswer,
                RefinedQuery = refined,
                Type = TurnType.ClarificationGiven
            });

            return (refined, selectedOptions);
        }

        // ─── Accessors ─────────────────────────────────────────
        public SessionContext Session => _session;
        public bool WaitingForAnswer => _session.WaitingForAnswer;

        // ─── Reset session ─────────────────────────────────────
        public void Reset()
        {
            _session = new SessionContext();
            Console.WriteLine("[Memory] Session reset");
        }

        // ─── Print history ─────────────────────────────────────
        public void PrintHistory()
        {
            Console.WriteLine("\n[Memory] ── Conversation History ──");
            foreach (var turn in _session.Turns)
                Console.WriteLine($"  Turn {turn.TurnNumber} [{turn.Type}]: {turn.UserInput}");
            Console.WriteLine("────────────────────────────────────\n");
        }

        // ─── Get conversation history for LLM ─────────────────
        public List<Message> GetConversationHistory()
        {
            var messages = new List<Message>();

            foreach (var turn in _session.Turns)
            {
                switch (turn.Type)
                {
                    case TurnType.RefinedQuery:
                    case TurnType.ClarificationGiven:
                        messages.Add(new Message { Role = "user", Content = turn.RefinedQuery ?? turn.UserInput });
                        if (!string.IsNullOrEmpty(turn.AssistantReply))
                            messages.Add(new Message { Role = "assistant", Content = turn.AssistantReply });
                        break;

                    case TurnType.ClarificationAsked:
                        // Skip — this was system asking a question, not a real LLM query
                        break;
                }
            }

            return messages;
        }

        // ─── Store assistant reply into last turn ──────────────
        public void SetAssistantReply(string reply)
        {
            var lastTurn = _session.Turns.LastOrDefault(t =>
                t.Type == TurnType.RefinedQuery ||
                t.Type == TurnType.ClarificationGiven);

            if (lastTurn != null)
                lastTurn.AssistantReply = reply;
        }
        public string GetHistory()
        {
            var sb = new StringBuilder();
            foreach (var turn in _session.Turns)
            {
                sb.AppendLine($"Turn {turn.TurnNumber} [{turn.Type}]:");
                sb.AppendLine($"  User: {turn.UserInput}");
                if (!string.IsNullOrEmpty(turn.AssistantReply))
                    sb.AppendLine($"  Assistant: {turn.AssistantReply}");
            }
            return sb.ToString();
        }
        public MemoryTurn GetLatestTurn()
        {
            return _session.Turns.LastOrDefault(); 
        }
        public MemoryChain GetCurrentChain()
        {
            return _session.GetCurrentChain();
        }

        public bool IsContineousContext(string originalPrompt, ExtractedEntity extracted)
        {
            var lastTurn = GetLatestTurn();
            // No prior context exists — cannot be continuous
            if (lastTurn?.ResolvedEntity == null)
                return false;

            // ── Case 1: User mentioned NO entity in new query ────────────
            // "Show attendance below 75%" — no name, just adding a filter
            // This is genuinely continuous — carry forward the session entity
            if (extracted.Name == null)
                return true;

            // ── Case 2: User mentioned an entity — must match prior one ──
            // "Show attendance for Student15" while session has Student12
            // Different entity = new context, not continuous
            if (!string.Equals(extracted.Name, lastTurn.ResolvedEntity.Name,
                               StringComparison.OrdinalIgnoreCase))
                return false;

            // ── Case 3: Same entity name — check type also matches ───────
            // Avoid "Student10" matching a "Teacher10" from a prior turn
            if (extracted.Type != EntityType.Unknown &&
                lastTurn.ResolvedEntity.Type != EntityType.Unknown &&
                extracted.Type != lastTurn.ResolvedEntity.Type)
                return false;

            // Same entity, same type — genuinely continuous
            return true;
        }

        public string BuildRefinedQuery(string originalPrompt, ExtractedEntity extracted)
        {
            if (extracted.Name == null) return originalPrompt;

            // ── Guard 1: filter/aggregate query — never inject entity ─────
            if (ParameterExtractor.IsAggregateOrFilterQuery(originalPrompt))
                return originalPrompt;

            // ── Guard 2: no entity to inject ─────────────────────────────
            if (string.IsNullOrEmpty(extracted.Name))
                return originalPrompt;

            // ── Replace pronoun if present ────────────────────────────────
            var words = originalPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var foundPronoun = words.FirstOrDefault(w => Pronouns.Contains(w));
            if (foundPronoun != null)
            {
                return Regex.Replace(
                    originalPrompt,
                    $@"\b{Regex.Escape(foundPronoun)}\b",
                    extracted.Name,
                    RegexOptions.IgnoreCase);
            }

            // ── Continuation query — append entity at end ─────────────────
            if (!originalPrompt.Contains(extracted.Name, StringComparison.OrdinalIgnoreCase))
                return $"{originalPrompt} for {extracted.Name}";

            // ── Entity already in prompt — return as is ───────────────────
            return originalPrompt;
        }

        
        public string GetHistory(HistoryMode mode = HistoryMode.CurrentChainOnly, bool includeSQL = true)
        {
            return mode switch
            {
                HistoryMode.CurrentChainOnly => BuildChainHistory(_session.GetCurrentChain(), includeSQL),
                HistoryMode.AllChains => BuildAllChainsHistory(),
                HistoryMode.RawTurns => BuildRawHistory(),
                _ => BuildChainHistory(_session.GetCurrentChain(), includeSQL)
            };
        }
        // ── Context switch logic ─────────────────────────────────────────
        private bool DetectContextSwitch(ExtractedEntity current)
        {
            var lastTurn = GetLatestTurn();
            var previous = lastTurn?.ResolvedEntity;

            if (previous == null) return false;  // first turn
            if (current.IsPronoun) return false;  // pronoun = same chain
            if (current.Type != previous.Type) return true;   // entity type changed
            if (current.IsScope != previous.IsScope) return true; // scope changed
            if (!string.Equals(current.Name, previous.Name,
                    StringComparison.OrdinalIgnoreCase)) return true; // different name

            return false;
        }

        // ── 1. CURRENT CHAIN ONLY (recommended for LLM prompt) ──────────────
        private string BuildChainHistory(MemoryChain? chain, bool includeSQL = true)
        {
            if (chain == null) return string.Empty;

            var sb = new StringBuilder();

            // ── Chain Header ─────────────────────────────────────────────
            sb.AppendLine($"[Context: {chain.Entity.Type} = {chain.Entity.Name}]");
            sb.AppendLine();

            // ── All Turns — lightweight ──────────────────────────────────
            foreach (var turn in chain.Turns)
            {
                sb.AppendLine($"Turn {turn.TurnNumber} [{turn.Type}]:");
                sb.AppendLine($"  User: {turn.RefinedQuery}");
            }

            if (includeSQL)
            {
                // ── Last Turn SQL Only — with length guard ───────────────────────
                var lastTurn = chain.Turns.LastOrDefault();
                if (lastTurn != null && !string.IsNullOrEmpty(lastTurn.GeneratedSQL))
                {
                    var sql = lastTurn.GeneratedSQL.Length > 1000
                     ? lastTurn.GeneratedSQL[..1000] + "\n-- [truncated]"
                     : lastTurn.GeneratedSQL;

                    sb.AppendLine();
                    sb.AppendLine("[LAST SQL]");
                    sb.AppendLine(sql);
                    sb.AppendLine("[END SQL]");
                }
            }
            return sb.ToString();
        }

        // ── 2. ALL CHAINS (debugging / full audit) ───────────────────────────
        private string BuildAllChainsHistory()
        {
            var sb = new StringBuilder();

            foreach (var chain in _session.GetAllChains())
            {
                sb.AppendLine($"══════════════════════════════════════════");
                sb.AppendLine($"Chain  : {chain.ChainID}");
                sb.AppendLine($"Entity : {chain.Entity.Type} = {chain.Entity.Name}");
                sb.AppendLine($"Status : {chain.Status}");
                sb.AppendLine($"══════════════════════════════════════════");
                sb.Append(BuildChainHistory(chain));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ── 3. RAW TURNS (your original — kept for backward compatibility) ────
        private string BuildRawHistory()
        {
            var sb = new StringBuilder();

            foreach (var turn in _session.Turns)
            {
                sb.AppendLine($"Turn {turn.TurnNumber} [{turn.Type}]:");
                sb.AppendLine($"  User: {turn.UserInput}");

                if (!string.IsNullOrEmpty(turn.AssistantReply))
                    sb.AppendLine($"  Assistant: {turn.AssistantReply}");
            }

            return sb.ToString();
        }
    }
}
