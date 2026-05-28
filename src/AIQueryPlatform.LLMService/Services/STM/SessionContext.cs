using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Services.IntentDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIQueryPlatform.LLMServiceOperator.Services.STM.EntityNameExtractor;

namespace AIQueryPlatform.LLMServiceOperator.Services.STM
{
    public class SessionContext
    {
        // Full conversation history
        public List<MemoryTurn> Turns { get; set; } = new();
        private readonly List<MemoryTurn> _turns = new();
        private readonly List<MemoryChain> _chains = new();
        private MemoryChain? _currentChain;

        // Current active entity focus
        public string? ActiveEntity { get; set; }  // "Student"
        public string? ActiveEntityName { get; set; }  // "ABC"
        public string? ActiveEntityID { get; set; }  // "1023" if resolved

        // What info has already been fetched
        public List<string> FetchedInfoTypes { get; set; } = new();
        // ["Academic", "Attendance"]

        // Waiting for clarification
        public bool WaitingForAnswer { get; set; }
        public string? OriginalQuery { get; set; }
        public VaguenessResult? VaguenessContext { get; set; }
        public List<ClarificationOption> PendingOptions { get; set; } = new();

        // ─── Helpers ──────────────────────────────────────────
        public MemoryTurn? LastTurn =>
            Turns.LastOrDefault();

        public bool HasActiveEntity =>
            !string.IsNullOrEmpty(ActiveEntityName);

        public void AddTurn(MemoryTurn turn)
        {
            turn.TurnNumber = Turns.Count + 1;
            Turns.Add(turn);
            // Also add to current active chain if exists
            _currentChain?.Turns.Add(turn);
        }

        // 1. FINALIZE CURRENT CHAIN
        //    Called when a context switch is detected.
        //    Marks current chain as complete and archives it.
        public void FinalizeCurrentChain()
        {
            if (_currentChain == null) return;

            _currentChain.Status = ChainStatus.Finalized;
            _currentChain.FinalizedAt = DateTime.UtcNow;

            // Archive it — already in _chains list
            // Optionally persist to DB here
            OnChainFinalized(_currentChain);

            _currentChain = null;  // clear active chain
        }

        // 2. START NEW CHAIN
        //    Called after FinalizeCurrentChain() on context switch,
        //    or on the very first turn.
        public void StartNewChain(ExtractedEntity entity)
        {
            // Safety — finalize any existing chain first
            if (_currentChain != null)
                FinalizeCurrentChain();

            _currentChain = new MemoryChain
            {
                Entity = entity,
                Status = ChainStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _chains.Add(_currentChain);
        }

        // 3. APPEND TO CURRENT CHAIN
        //    Called when same entity continues — no context switch.
        //    Updates the entity in case of refinement (e.g. scope change).
        public void AppendToCurrentChain(string refinedQuery, ExtractedEntity current)
        {
            // If no chain exists yet, start one automatically
            if (_currentChain == null)
            {
                StartNewChain(current);
                return;
            }

            // Update entity on chain if refined (e.g. name was resolved from pronoun)
            if (!current.IsPronoun)
                _currentChain.Entity = current;
        }

        // Get current active chain
        public MemoryChain? GetCurrentChain() => _currentChain;

        // Get all finalized chains
        public IReadOnlyList<MemoryChain> GetFinalizedChains()
            => _chains.Where(c => c.Status == ChainStatus.Finalized).ToList();

        // Get full chain history
        public IReadOnlyList<MemoryChain> GetAllChains()
            => _chains.AsReadOnly();

        // Hook — override or subscribe to persist chain to DB
        private void OnChainFinalized(MemoryChain chain)
        {
            // Example:
            // await _repository.SaveChainAsync(chain);
            Console.WriteLine($"Chain finalized: {chain.ChainID} | Entity: {chain.Entity.Name} | Turns: {chain.Turns.Count}");
        }
    }
}
