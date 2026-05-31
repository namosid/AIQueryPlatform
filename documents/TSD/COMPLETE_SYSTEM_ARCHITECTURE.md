# AIQueryPlatform - Complete System Architecture & Flow
**Version:** 1.0  
**Date:** May 30, 2026  
**Purpose:** End-to-End System Architecture and Sequence Diagrams

---

## Table of Contents
1. [System Architecture Overview](#1-system-architecture-overview)
2. [Complete Sequence Diagram - Happy Path](#2-complete-sequence-diagram---happy-path)
3. [Complete Sequence Diagram - Error Recovery Flow](#3-complete-sequence-diagram---error-recovery-flow)
4. [Complete Sequence Diagram - Clarification Flow](#4-complete-sequence-diagram---clarification-flow)
5. [Layer-by-Layer Architecture](#5-layer-by-layer-architecture)
6. [Data Flow Architecture](#6-data-flow-architecture)

---

# 1. System Architecture Overview

## 1.1 High-Level System Architecture (4-Layer Stack)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER (Widget)                               │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │  React 18 Widget (Shadow DOM Isolated)                                        │ │
│  │  • QueryInput Component (natural language input)                              │ │
│  │  • ResultsTable Component (query results display)                             │ │
│  │  • TokenUsageTracker Component (cost monitoring)                              │ │
│  │  • WebSocket Connection (real-time streaming)                                 │ │
│  │  • State Management (Zustand)                                                 │ │
│  └───────────────────────┬───────────────────────────────────────────────────────┘ │
└────────────────────────────┼─────────────────────────────────────────────────────────┘
                             │ HTTPS/WSS
                             │ POST /api/query/natural
                             │ WS /api/query/stream
                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            APPLICATION LAYER (API)                                  │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │  ASP.NET Core Web API (.NET 8)                                                │ │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │  │  MIDDLEWARE PIPELINE                                                     │ │ │
│  │  │  1. ExceptionHandlingMiddleware                                         │ │ │
│  │  │  2. TenantResolutionMiddleware (X-Tenant-ID header → TenantContext)    │ │ │
│  │  │  3. RateLimitingMiddleware (per-tenant quotas)                         │ │ │
│  │  │  4. QuotaEnforcementMiddleware (token usage limits)                    │ │ │
│  │  └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │  │  CONTROLLERS                                                             │ │ │
│  │  │  • QueryController                                                       │ │ │
│  │  │    - POST /api/query/natural → NaturalLanguageQuery()                  │ │ │
│  │  │    - GET /api/query/stream → StreamQuery() (SSE/WebSocket)            │ │ │
│  │  │  • ConversationsController (history management)                        │ │ │
│  │  │  • TokenUsageController (usage tracking)                               │ │ │
│  │  │  • TenantController (tenant config)                                    │ │ │
│  │  └─────────────────────────────────────────────────────────────────────────┘ │ │
│  └───────────────────────┬───────────────────────────────────────────────────────┘ │
└────────────────────────────┼─────────────────────────────────────────────────────────┘
                             │ In-Process Call
                             │ LLMServicePipe.ProcessQuery()
                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        INTELLIGENCE LAYER (LLMService)                              │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │  AIQueryPlatform.LLMService Library                                           │ │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │  │  LLMServicePipe (ORCHESTRATOR)                                           │ │ │
│  │  │  • Entry point: ProcessQuery(userPrompt, tenant)                        │ │ │
│  │  │  • Coordinates: STM, Cache, Vector Search, LLM, Validation             │ │ │
│  │  └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                                │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │ │
│  │  │ Short-Term       │  │ Intent Detection │  │ Vector Search            │   │ │
│  │  │ Memory (STM)     │  │ & Clarification  │  │ (Qdrant)                 │   │ │
│  │  ├──────────────────┤  ├──────────────────┤  ├──────────────────────────┤   │ │
│  │  │• Conversation    │  │• IntentAnalyzer  │  │• Schema Chunking         │   │ │
│  │  │  history         │  │• Vagueness       │  │• Embedding Generation    │   │ │
│  │  │• Entity tracking │  │  detection       │  │• Similarity Search       │   │ │
│  │  │• Follow-up       │  │• Clarification   │  │• Top-K Retrieval         │   │ │
│  │  │  detection       │  │  Agent           │  │• Dependency Resolution   │   │ │
│  │  │• Context         │  │• Question        │  │                          │   │ │
│  │  │  enrichment      │  │  generation      │  │  [Qdrant Cloud API]      │   │ │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────────────┘   │ │
│  │                                                                                │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │ │
│  │  │ Semantic Cache   │  │ LLM Service      │  │ Query Classification     │   │ │
│  │  ├──────────────────┤  ├──────────────────┤  ├──────────────────────────┤   │ │
│  │  │• Vector storage  │  │• Azure OpenAI    │  │• Type detection          │   │ │
│  │  │• Similarity      │  │  GPT-4/GPT-4o    │  │  (trend/predictive)      │   │ │
│  │  │  scoring (0.85+) │  │• Prompt builder  │  │• Complexity scoring      │   │ │
│  │  │• Cache hit/miss  │  │• Response parser │  │• Rule injection          │   │ │
│  │  │• 92% hit rate    │  │• Token tracking  │  │                          │   │ │
│  │  │                  │  │                  │  │                          │   │ │
│  │  │  [SQL Server]    │  │  [Azure OpenAI]  │  │                          │   │ │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────────────┘   │ │
│  └───────────────────────┬───────────────────────────────────────────────────────┘ │
└────────────────────────────┼─────────────────────────────────────────────────────────┘
                             │ In-Process Call
                             │ SqlValidationPipeline.ValidateAsync()
                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         VALIDATION LAYER (SqlValidator)                             │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │  AIQueryPlatform.SqlValidator Library                                         │ │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │  │  SqlValidationPipeline (4-LAYER ORCHESTRATOR)                            │ │ │
│  │  │  • Sequential validation with fail-fast                                  │ │ │
│  │  │  • Structured error reporting with fix hints                            │ │ │
│  │  └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                                │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────┐ │ │
│  │  │ Layer 1:        │  │ Layer 2:        │  │ Layer 3:        │  │ Layer 4:│ │ │
│  │  │ SyntaxValidator │  │ SchemaValidator │  │ LLMSelfCheck    │  │ DryRun  │ │ │
│  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤  ├─────────┤ │ │
│  │  │• SELECT check   │  │• Table exist    │  │• Semantic       │  │• SHOW-  │ │ │
│  │  │• FROM check     │  │• Column exist   │  │  validation     │  │  PLAN   │ │ │
│  │  │• Parentheses    │  │• JOIN valid     │  │• Claude API     │  │• Exec   │ │ │
│  │  │• String quotes  │  │• FK relations   │  │• Logical        │  │  plan   │ │ │
│  │  │• JOIN syntax    │  │• Fuzzy matching │  │  issues         │  │• Zero   │ │ │
│  │  │                 │  │• Typo suggest   │  │• (Optional)     │  │  data   │ │ │
│  │  │  <10ms          │  │  <30ms          │  │  <2s            │  │  <200ms │ │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────┘ │ │
│  │         │                     │                     │                  │      │ │
│  │         └─────────────────────┼─────────────────────┼──────────────────┘      │ │
│  │                               │                     │                         │ │
│  │                         ┌─────▼─────┐         ┌────▼────────────────────┐    │ │
│  │                         │ Database  │         │ Anthropic Claude API    │    │ │
│  │                         │ Schema    │         │ (Semantic Validation)   │    │ │
│  │                         │ (Parsed)  │         └─────────────────────────┘    │ │
│  │                         └───────────┘                                         │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────┬──────────────────────────────────────────────────────────┘
                             │ Return: ValidationResult
                             │ (IsValid, FixHint, ErrorCodes)
                             ▼
                      [Back to LLMService]
                      [If invalid: Retry with fix hint]
                      [If valid: Execute SQL]
```

---

# 2. Complete Sequence Diagram - Happy Path

## User Query: "Show attendance for student10" (Cache Miss, Valid SQL)

```
User      Widget        API           LLMService         STM      Intent    SemanticCache  Qdrant    LLM      SqlValidator  Database
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 ├─Type: "Show attendance for student10"─►│              │          │            │           │        │            │            │
 │           ├─setState(loading)           │              │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           ├─POST /api/query/natural────►│              │          │            │           │        │            │            │
 │           │   {                          │              │          │            │           │        │            │            │
 │           │     query: "Show...",        │              │          │            │           │        │            │            │
 │           │     tenantId: "demo"         │              │          │            │           │        │            │            │
 │           │   }                          │              │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ├─[Middleware Pipeline]          │          │            │           │        │            │            │
 │           │           │  1. TenantResolution           │          │            │           │        │            │            │
 │           │           │     (X-Tenant-ID → TenantContext)        │            │           │        │            │            │
 │           │           │  2. RateLimiting                          │            │           │        │            │            │
 │           │           │     (Check quota: 100 req/min)            │            │           │        │            │            │
 │           │           │  3. QuotaEnforcement                      │            │           │        │            │            │
 │           │           │     (Token budget: 50K/day)               │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ├─QueryController.NaturalLanguageQuery()   │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ├─ProcessQuery("Show...", tenant)──────────►│          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─ReadSchema(tenant.SchemaFile)       │           │        │            │            │
 │           │           │                 │  (Load DemoSchema.txt)              │           │        │            │            │
 │           │           │                 ◄─────────────┤          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─ShortTermMemory.GetLatestTurn()────►│            │           │        │            │            │
 │           │           │                 │              ├─Check duplicate query│            │           │        │            │            │
 │           │           │                 ◄──────────────┤ (not duplicate)      │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─EntityNameExtractor.Extract()       │            │           │        │            │            │
 │           │           │                 │  (Extract "student10")              │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─DetermineQueryType()                │            │           │        │            │            │
 │           │           │                 │  ├─Check if waiting for clarification│           │           │        │            │            │
 │           │           │                 │  ├─Check if follow-up (IsFollowUp())─────────────►│           │        │            │            │
 │           │           │                 │  │  (returns false - new query)     │            │           │        │            │            │
 │           │           │                 │  └─Check if continuous context      │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─IntentAnalyzer.Analyze()────────────────────────►│           │        │            │            │
 │           │           │                 │              │          ├─Check vague terms      │           │        │            │            │
 │           │           │                 │              │          ├─Check specific terms   │           │        │            │            │
 │           │           │                 │              │          ├─Vagueness score: 0.0   │           │        │            │            │
 │           │           │                 ◄──────────────────────────┤ (IsVague: false)      │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─SemanticCacheService.GetCacheSearchAsync()──────────────────►│        │            │            │
 │           │           │                 │              │          │            ├─Generate embedding     │        │            │            │
 │           │           │                 │              │          │            ├─Vector similarity search│       │            │            │
 │           │           │                 │              │          │            ├─Cosine similarity < 0.85│       │            │            │
 │           │           │                 ◄────────────────────────────────────┤ (Cache MISS)            │       │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─QdrantService.SearchAsync()────────────────────────────────────────►│        │            │            │
 │           │           │                 │              │          │            │           ├─Generate query embedding│            │            │
 │           │           │                 │              │          │            │           ├─Search vectors (top-5)   │            │            │
 │           │           │                 │              │          │            │           ├─Found: Students, Attendance tables│   │            │
 │           │           │                 │              │          │            │           ├─Resolve dependencies (FK: StudentId)│ │            │
 │           │           │                 ◄────────────────────────────────────────────────┤ SearchOutput{schema, entities}│       │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─LlmService.AskAsync(schema, query)──────────────────────────────────────────►│            │            │
 │           │           │                 │              │          │            │           │        ├─Build prompt with schema│   │            │
 │           │           │                 │              │          │            │           │        ├─Inject rules (T-SQL)    │   │            │
 │           │           │                 │              │          │            │           │        ├─Inject enum context     │   │            │
 │           │           │                 │              │          │            │           │        ├─Call Azure OpenAI GPT-4 │   │            │
 │           │           │                 │              │          │            │           │        │  (temp: 0.0, max: 500)  │   │            │
 │           │           │                 │              │          │            │           │        ├─Parse SQL from response │   │            │
 │           │           │                 ◄────────────────────────────────────────────────────────┤ SQL: "SELECT..."         │   │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─SqlValidationPipeline.ValidateAsync(sql)───────────────────────────────────────────►│            │
 │           │           │                 │              │          │            │           │        │            ├─Layer 1: Syntax (10ms)│
 │           │           │                 │              │          │            │           │        │            │  ✓ SELECT present     │
 │           │           │                 │              │          │            │           │        │            │  ✓ FROM present       │
 │           │           │                 │              │          │            │           │        │            │  ✓ Parentheses OK     │
 │           │           │                 │              │          │            │           │        │            │            │            │
 │           │           │                 │              │          │            │           │        │            ├─Layer 2: Schema (25ms)│
 │           │           │                 │              │          │            │           │        │            │  ✓ Table 'Students' found│
 │           │           │                 │              │          │            │           │        │            │  ✓ Table 'Attendance' found│
 │           │           │                 │              │          │            │           │        │            │  ✓ Columns valid      │
 │           │           │                 │              │          │            │           │        │            │  ✓ JOIN keys valid    │
 │           │           │                 │              │          │            │           │        │            │            │            │
 │           │           │                 │              │          │            │           │        │            ├─Layer 4: Dry-Run (180ms)│
 │           │           │                 │              │          │            │           │        │            ├─SET SHOWPLAN_ALL ON──────►│
 │           │           │                 │              │          │            │           │        │            ├─ExecuteReader(sql)───────►│
 │           │           │                 │              │          │            │           │        │            │            ├─Generate plan (no data)
 │           │           │                 │              │          │            │           │        │            ◄────────────┤ Plan OK   │
 │           │           │                 │              │          │            │           │        │            ├─SET SHOWPLAN_ALL OFF─────►│
 │           │           │                 ◄────────────────────────────────────────────────────────────────────────┤ ValidationResult{IsValid: true}│
 │           │           │                 │              │          │            │           │        │            │            │            │
 │           │           │                 ├─SaveToSemanticCache(query, sql)────────────────────────────►│        │            │            │
 │           │           │                 │              │          │            ├─Store: query + SQL + embedding  │            │            │
 │           │           │                 ◄────────────────────────────────────┤            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           │                 ├─ShortTermMemory.UpdateContext()────►│            │           │        │            │            │
 │           │           │                 │              ├─Store turn: {       │            │           │        │            │            │
 │           │           │                 │              │  UserInput: "Show..."│            │           │        │            │            │
 │           │           │                 │              │  GeneratedSQL: "SELECT..."        │           │        │            │            │
 │           │           │                 │              │  Entity: "student10" │            │           │        │            │            │
 │           │           │                 │              │}                     │            │           │        │            │            │
 │           │           │                 ◄──────────────┤            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ◄─LLMResponse{SQL: "SELECT...", Type: SQL}──┤          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ├─ExecuteQuery(sql)───────────────────────────────────────────────────────────────────────────────────────────────►│
 │           │           │                 │              │          │            │           │        │            │            ├─Execute SQL│
 │           │           │                 │              │          │            │           │        │            │            ├─Return rows│
 │           │           ◄─────────────────────────────────────────────────────────────────────────────────────────────────────────┤ ResultSet│
 │           │           │                 │              │          │            │           │        │            │            │
 │           │           ├─TrackTokenUsage()              │          │            │           │        │            │            │
 │           │           │  (Prompt: 3,250 tokens)        │          │            │           │        │            │            │
 │           │           │  (Completion: 50 tokens)       │          │            │           │        │            │            │
 │           │           │  (Total: 3,300 tokens)         │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           ◄─200 OK────┤                 │              │          │            │           │        │            │            │
 │           │  {                           │              │          │            │           │        │            │            │
 │           │    sql: "SELECT...",         │              │          │            │           │        │            │            │
 │           │    results: [...],           │              │          │            │           │        │            │            │
 │           │    tokenUsage: {...}         │              │          │            │           │        │            │            │
 │           │  }                           │              │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 │           ├─setState(success)            │              │          │            │           │        │            │            │
 │           ├─Render ResultsTable          │              │          │            │           │        │            │            │
 │           ├─Update TokenUsage Badge      │              │          │            │           │        │            │            │
 │           │           │                 │              │          │            │           │        │            │            │
 ◄─Display───┤           │                 │              │          │            │           │        │            │            │
   Results                │                 │              │          │            │           │        │            │            │

[Total Time: ~5s]
  - Middleware: 5ms
  - LLM Processing: 3,200ms
  - Validation: 215ms
  - SQL Execution: 150ms
  - Other: 1,430ms
```

---

# 3. Complete Sequence Diagram - Error Recovery Flow

## User Query: "SELECT * FROM Studnets" (Typo in table name)

```
User      Widget        API           LLMService         SqlValidator    LLM         Database
 │           │           │                 │                    │           │             │
 │           │           │                 │                    │           │             │
 ├─Type: "Show all students"─────────────►│                    │           │             │
 │           ├─POST /api/query/natural────►│                    │           │             │
 │           │           │                 │                    │           │             │
 │           │           ├─[Middleware OK]─┤                    │           │             │
 │           │           │                 │                    │           │             │
 │           │           ├─ProcessQuery()──────────────────────►│           │             │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─[Load Schema, Check STM, Check Cache...]    │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─LlmService.AskAsync()──────────────────────►│
 │           │           │                 │                    │           ├─Generate SQL│
 │           │           │                 ◄────────────────────────────────┤             │
 │           │           │                 │  SQL: "SELECT * FROM Studnets" (TYPO!)      │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─ValidateAsync(sql)────────────►│             │
 │           │           │                 │                    ├─Layer 1: Syntax         │
 │           │           │                 │                    │  ✓ PASS  │             │
 │           │           │                 │                    │           │             │
 │           │           │                 │                    ├─Layer 2: Schema         │
 │           │           │                 │                    │  ✗ Table 'Studnets' not found│
 │           │           │                 │                    │  ├─Levenshtein("Studnets", "Students") = 1│
 │           │           │                 │                    │  ├─Generate suggestion │
 │           │           │                 ◄────────────────────┤           │             │
 │           │           │                 │  {                 │           │             │
 │           │           │                 │    IsValid: false, │           │             │
 │           │           │                 │    FailedLayer: Schema,        │             │
 │           │           │                 │    FixHint: "[SCH001] Table 'Studnets' does not exist. → Fix: Did you mean 'Students'?"│
 │           │           │                 │  }                 │           │             │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─[RETRY LOGIC - Attempt 1/3]   │             │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─LlmService.FixSQL(originalSQL, fixHint)─────►│
 │           │           │                 │  Prompt: "The following SQL has errors:     │
 │           │           │                 │           SELECT * FROM Studnets             │
 │           │           │                 │           Issues: [SCH001] Table 'Studnets' does not exist. → Did you mean 'Students'?│
 │           │           │                 │           Please fix the SQL."               │
 │           │           │                 │                    │           ├─Self-correct│
 │           │           │                 ◄────────────────────────────────┤             │
 │           │           │                 │  SQL: "SELECT * FROM Students" (FIXED!)     │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─ValidateAsync(fixedSQL)───────►│             │
 │           │           │                 │                    ├─Layer 1: Syntax         │
 │           │           │                 │                    │  ✓ PASS  │             │
 │           │           │                 │                    ├─Layer 2: Schema         │
 │           │           │                 │                    │  ✓ PASS (table found)  │
 │           │           │                 │                    ├─Layer 4: Dry-Run        │
 │           │           │                 │                    │  ✓ PASS  │             │
 │           │           │                 ◄────────────────────┤           │             │
 │           │           │                 │  { IsValid: true } │           │             │
 │           │           │                 │                    │           │             │
 │           │           │                 ├─[Save to cache, update STM...]│             │
 │           │           │                 │                    │           │             │
 │           │           ◄─LLMResponse{SQL: "SELECT...", Type: SQL}───────┤             │
 │           │           │                 │                    │           │             │
 │           │           ├─ExecuteQuery(fixedSQL)──────────────────────────────────────►│
 │           │           ◄─────────────────────────────────────────────────────────────┤
 │           │           │                 │                    │           │  ResultSet │
 │           ◄─200 OK────┤                 │                    │           │             │
 │           │           │                 │                    │           │             │
 ◄─Display───┤           │                 │                    │           │             │
   Results                │                 │                    │           │             │

[Total Time: ~7s]
  - Initial validation: 35ms (failed at Layer 2)
  - LLM self-correction: 3,500ms
  - Re-validation: 215ms (all layers pass)
  - SQL Execution: 150ms
  - Other: 3,100ms

[Auto-Fix Success Rate: 82%]
```

---

# 4. Complete Sequence Diagram - Clarification Flow

## User Query: "Show me the report" (Vague query)

```
User      Widget        API           LLMService         STM      Intent    Clarification  User
 │           │           │                 │              │          │            │           │
 │           │           │                 │              │          │            │           │
 ├─Type: "Show me the report"─────────────►│              │          │            │           │
 │           ├─POST /api/query/natural────►│              │          │            │           │
 │           │           │                 │              │          │            │           │
 │           │           ├─ProcessQuery()──────────────────►         │            │           │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─[Load Schema, Check STM, Extract Entity]       │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─IntentAnalyzer.Analyze("Show me the report")──►│
 │           │           │                 │              │          ├─Check vague terms    │
 │           │           │                 │              │          │  Found: "report"     │
 │           │           │                 │              │          ├─Check specific terms │
 │           │           │                 │              │          │  Found: none         │
 │           │           │                 │              │          ├─Calculate vagueness  │
 │           │           │                 │              │          │  Score: 0.2 (VAGUE!) │
 │           │           │                 ◄──────────────────────────┤                     │
 │           │           │                 │  {          │          │            │           │
 │           │           │                 │    IsVague: true,       │            │           │
 │           │           │                 │    VagueTerms: ["report"],          │           │
 │           │           │                 │    Entity: "Unknown"    │            │           │
 │           │           │                 │  }          │          │            │           │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─ClarificationAgent.GenerateQuestion(vagueness)──────────►│
 │           │           │                 │              │          │            ├─Map "report" to options│
 │           │           │                 │              │          │            ├─Build question with options│
 │           │           │                 ◄──────────────────────────────────────┤           │
 │           │           │                 │  {          │          │            │           │
 │           │           │                 │    Question: "Which report would you like to see? Please select:",│
 │           │           │                 │    Options: [                       │            │           │
 │           │           │                 │      "1. Attendance Report",        │            │           │
 │           │           │                 │      "2. Fee Report",               │            │           │
 │           │           │                 │      "3. Exam Report",              │            │           │
 │           │           │                 │      "4. Student Report"            │            │           │
 │           │           │                 │    ]                                │            │           │
 │           │           │                 │  }          │          │            │           │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─ShortTermMemory.SetWaitingForAnswer()────────►│            │
 │           │           │                 │              ├─Store clarification state      │            │
 │           │           │                 │              │  {                             │            │
 │           │           │                 │              │    OriginalQuery: "Show...",   │            │
 │           │           │                 │              │    Options: [...],             │            │
 │           │           │                 │              │    WaitingForAnswer: true      │            │
 │           │           │                 │              │  }                             │            │
 │           │           │                 ◄──────────────┤            │           │
 │           │           │                 │              │          │            │           │
 │           │           ◄─LLMResponse{    │              │          │            │           │
 │           │           │   Type: CLARIFICATION,         │          │            │           │
 │           │           │   Message: "Which report would you like to see? Please select:\n1. Attendance Report\n2. Fee Report\n3. Exam Report\n4. Student Report"│
 │           │           │ }               │              │          │            │           │
 │           │           │                 │              │          │            │           │
 │           ◄─200 OK────┤                 │              │          │            │           │
 │           ├─setState(clarification)     │              │          │            │           │
 │           ├─Render ClarificationDialog  │              │          │            │           │
 │           │  with 4 option buttons      │              │          │            │           │
 │           │           │                 │              │          │            │           │
 ◄─Display───┤           │                 │              │          │            │           │
   Dialog                 │                 │              │          │            │           │
 │           │           │                 │              │          │            │           │
 ├─Click: "1. Attendance Report"──────────►│              │          │            │           │
 │           ├─POST /api/query/natural────►│              │          │            │           │
 │           │   { query: "1" }            │              │          │            │           │
 │           │           │                 │              │          │            │           │
 │           │           ├─ProcessQuery("1")──────────────►         │            │           │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─DetermineQueryType()   │            │           │
 │           │           │                 │  ├─Check: memory.WaitingForAnswer?  │           │
 │           │           │                 │  │  → YES! User responding to clarification    │
 │           │           │                 │  │           │          │            │           │
 │           │           │                 ├─ShortTermMemory.ResolveClarification("1")─────►│
 │           │           │                 │              ├─Parse selection: "1" → "Attendance Report"│
 │           │           │                 │              ├─Build refined query │            │           │
 │           │           │                 │              │  "Show attendance report"       │           │
 │           │           │                 │              ├─Clear waiting state │            │           │
 │           │           │                 ◄──────────────┤  RefinedQuery: "Show attendance report"    │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─[Continue normal flow: Cache check, Vector search, LLM, Validation...]│
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─LlmService.AskAsync("Show attendance report")  │           │
 │           │           │                 │              │          │            │           │
 │           │           │                 ├─[... validation, execution ...]    │           │
 │           │           │                 │              │          │            │           │
 │           │           ◄─LLMResponse{    │              │          │            │           │
 │           │           │   SQL: "SELECT * FROM Attendance...",    │            │           │
 │           │           │   Type: SQL     │              │          │            │           │
 │           │           │ }               │              │          │            │           │
 │           │           │                 │              │          │            │           │
 │           ◄─200 OK────┤                 │              │          │            │           │
 │           │           │                 │              │          │            │           │
 ◄─Display───┤           │                 │              │          │            │           │
   Results                │                 │              │          │            │           │

[Clarification Flow]
  Turn 1: Vague query detected → Ask clarification (response time: ~500ms)
  Turn 2: User selects option → Refined query processed (response time: ~4s)
  [Total 2 turns with user interaction]
```

---

# 5. Layer-by-Layer Architecture

## 5.1 Widget Layer (React + Shadow DOM)

```
┌────────────────────────────────────────────────────────────────────────┐
│                      WIDGET LAYER (React 18)                           │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Shadow DOM Isolation                                             │ │
│  │  • Prevents CSS leakage to/from host page                        │ │
│  │  • Scoped styles with styled-components                          │ │
│  │  • Event isolation                                                │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  State Management (Zustand)                                       │ │
│  │                                                                    │ │
│  │  interface WidgetState {                                          │ │
│  │    query: string                                                  │ │
│  │    results: QueryResult[]                                         │ │
│  │    loading: boolean                                               │ │
│  │    error: string | null                                           │ │
│  │    tokenUsage: TokenUsage                                         │ │
│  │    conversationHistory: ConversationTurn[]                        │ │
│  │    clarificationPending: ClarificationQuestion | null             │ │
│  │  }                                                                 │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Component Hierarchy                                              │ │
│  │                                                                    │ │
│  │  <AIQueryWidget>                          (Root container)        │ │
│  │    ├─ <QueryInput />                      (NL input field)        │ │
│  │    │   ├─ onSubmit → dispatch(submitQuery)                       │ │
│  │    │   └─ disabled={loading}                                      │ │
│  │    │                                                              │ │
│  │    ├─ <ClarificationDialog />             (Conditional render)    │ │
│  │    │   ├─ shows when clarificationPending !== null               │ │
│  │    │   ├─ options: string[]                                      │ │
│  │    │   └─ onSelect → dispatch(selectOption)                      │ │
│  │    │                                                              │ │
│  │    ├─ <ResultsTable />                    (Query results)         │ │
│  │    │   ├─ columns: ColumnDef[]                                   │ │
│  │    │   ├─ data: results                                          │ │
│  │    │   └─ pagination, sorting, filtering                         │ │
│  │    │                                                              │ │
│  │    ├─ <ConversationHistory />             (Past turns)            │ │
│  │    │   └─ turns: { query, sql, timestamp }[]                     │ │
│  │    │                                                              │ │
│  │    └─ <TokenUsageBadge />                 (Cost tracker)          │ │
│  │        ├─ promptTokens: number                                   │ │
│  │        ├─ completionTokens: number                               │ │
│  │        └─ totalCost: number                                      │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  API Client (services/api.ts)                                     │ │
│  │                                                                    │ │
│  │  class APIClient {                                                │ │
│  │    private baseURL = window.AIQueryConfig.apiUrl                 │ │
│  │    private tenantId = window.AIQueryConfig.tenantId              │ │
│  │                                                                    │ │
│  │    async submitQuery(query: string): Promise<QueryResponse> {    │ │
│  │      return fetch(`${this.baseURL}/api/query/natural`, {         │ │
│  │        method: 'POST',                                            │ │
│  │        headers: {                                                 │ │
│  │          'Content-Type': 'application/json',                      │ │
│  │          'X-Tenant-ID': this.tenantId                             │ │
│  │        },                                                          │ │
│  │        body: JSON.stringify({ query })                            │ │
│  │      })                                                            │ │
│  │    }                                                               │ │
│  │                                                                    │ │
│  │    async streamQuery(query: string): AsyncIterator<Chunk> {      │ │
│  │      // WebSocket or Server-Sent Events for streaming            │ │
│  │    }                                                               │ │
│  │  }                                                                 │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

## 5.2 API Layer (ASP.NET Core)

```
┌────────────────────────────────────────────────────────────────────────┐
│                      API LAYER (.NET 8)                                │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  MIDDLEWARE PIPELINE (app.UseMiddleware<T>())                     │ │
│  │                                                                    │ │
│  │  HTTP Request                                                      │ │
│  │       ↓                                                            │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ 1. ExceptionHandlingMiddleware                              │  │ │
│  │  │    • Catch all exceptions                                   │  │ │
│  │  │    • Log to Application Insights                            │  │ │
│  │  │    • Return 500 with error details                          │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │       ↓                                                            │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ 2. TenantResolutionMiddleware                               │  │ │
│  │  │    • Read X-Tenant-ID header                                │  │ │
│  │  │    • Query tenant config from DB                            │  │ │
│  │  │    • Populate TenantContext (scoped service)                │  │ │
│  │  │    • Validate tenant exists and is active                   │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │       ↓                                                            │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ 3. RateLimitingMiddleware                                   │  │ │
│  │  │    • Check Redis: GET rate_limit:{tenantId}:{endpoint}     │  │ │
│  │  │    • If count > limit (100/min): Return 429                 │  │ │
│  │  │    • Else: INCR count, EXPIRE 60s                           │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │       ↓                                                            │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ 4. QuotaEnforcementMiddleware                               │  │ │
│  │  │    • Query TokenUsage table for today's usage               │  │ │
│  │  │    • If totalTokens > tenantQuota (50K/day): Return 429     │  │ │
│  │  │    • Add X-Token-Usage-Remaining header                     │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │       ↓                                                            │ │
│  │  Controller Action                                                 │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  CONTROLLERS                                                       │ │
│  │                                                                    │ │
│  │  [ApiController]                                                  │ │
│  │  [Route("api/[controller]")]                                      │ │
│  │  public class QueryController : ControllerBase {                 │ │
│  │                                                                    │ │
│  │    [HttpPost("natural")]                                          │ │
│  │    public async Task<IActionResult> NaturalLanguageQuery(        │ │
│  │      [FromBody] QueryRequest request) {                          │ │
│  │                                                                    │ │
│  │      // 1. Get tenant context (injected by middleware)            │ │
│  │      var tenant = _tenantContext.Tenant;                          │ │
│  │                                                                    │ │
│  │      // 2. Call LLM Service                                       │ │
│  │      var llmResponse = await _llmServicePipe.ProcessQuery(       │ │
│  │        request.Query,                                             │ │
│  │        new TenantData {                                           │ │
│  │          TenantId = tenant.Id,                                    │ │
│  │          SchemaFile = tenant.SchemaFile,                          │ │
│  │          MappingFile = tenant.MappingFile                         │ │
│  │        }                                                           │ │
│  │      );                                                            │ │
│  │                                                                    │ │
│  │      // 3. Handle response type                                   │ │
│  │      if (llmResponse.Type == ResponseType.CLARIFICATION) {       │ │
│  │        return Ok(new {                                            │ │
│  │          type = "clarification",                                  │ │
│  │          message = llmResponse.Message                            │ │
│  │        });                                                         │ │
│  │      }                                                             │ │
│  │                                                                    │ │
│  │      if (llmResponse.Type == ResponseType.ERROR) {               │ │
│  │        return BadRequest(new { error = llmResponse.Message });   │ │
│  │      }                                                             │ │
│  │                                                                    │ │
│  │      // 4. Execute SQL query                                      │ │
│  │      var results = await _queryExecutionService.ExecuteAsync(    │ │
│  │        llmResponse.SQL,                                           │ │
│  │        tenant.ConnectionString                                    │ │
│  │      );                                                            │ │
│  │                                                                    │ │
│  │      // 5. Track token usage                                      │ │
│  │      await _tokenUsageService.TrackAsync(new TokenUsageRecord {  │ │
│  │        TenantId = tenant.Id,                                      │ │
│  │        PromptTokens = llmResponse.PromptTokens,                   │ │
│  │        CompletionTokens = llmResponse.CompletionTokens,           │ │
│  │        Timestamp = DateTime.UtcNow                                │ │
│  │      });                                                           │ │
│  │                                                                    │ │
│  │      // 6. Return results                                         │ │
│  │      return Ok(new {                                              │ │
│  │        sql = llmResponse.SQL,                                     │ │
│  │        results = results,                                         │ │
│  │        tokenUsage = new {                                         │ │
│  │          promptTokens = llmResponse.PromptTokens,                 │ │
│  │          completionTokens = llmResponse.CompletionTokens,         │ │
│  │          totalTokens = llmResponse.TotalTokens                    │ │
│  │        }                                                           │ │
│  │      });                                                           │ │
│  │    }                                                               │ │
│  │  }                                                                 │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  DEPENDENCY INJECTION (Program.cs)                                │ │
│  │                                                                    │ │
│  │  builder.Services.AddScoped<TenantContext>();                     │ │
│  │  builder.Services.AddScoped<ILLMServicePipe, LLMServicePipe>();   │ │
│  │  builder.Services.AddScoped<IQueryExecutionService, ...>();       │ │
│  │  builder.Services.AddScoped<ITokenUsageService, ...>();           │ │
│  │  builder.Services.AddHttpClient<ILlmService, LlmService>();       │ │
│  │                                                                    │ │
│  │  // Middleware order matters!                                     │ │
│  │  app.UseMiddleware<ExceptionHandlingMiddleware>();                │ │
│  │  app.UseMiddleware<TenantResolutionMiddleware>();                 │ │
│  │  app.UseMiddleware<RateLimitingMiddleware>();                     │ │
│  │  app.UseMiddleware<QuotaEnforcementMiddleware>();                 │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

## 5.3 LLMService Layer (Intelligence)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   LLM SERVICE LAYER (Intelligence)                     │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  LLMServicePipe (Main Orchestrator)                               │ │
│  │                                                                    │ │
│  │  public async Task<LLMResponse> ProcessQuery(                     │ │
│  │    string userPrompt,                                             │ │
│  │    TenantData tenant) {                                           │ │
│  │                                                                    │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 1: Load Schema                                      │  │ │
│  │    │ var schema = FileSchemaService.ReadSchema(tenant.File)   │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 2: Initialize Services                              │  │ │
│  │    │ • EmbeddingService (text-embedding-ada-002)              │  │ │
│  │    │ • QdrantService (vector DB client)                       │  │ │
│  │    │ • SemanticCacheService (SQL cache)                       │  │ │
│  │    │ • ShortTermMemory (conversation state)                   │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 3: Check Duplicate Query                            │  │ │
│  │    │ if (memory.GetLatestTurn().UserInput == userPrompt)      │  │ │
│  │    │   return CachedResponse()                                │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 4: Handle Special Commands                          │  │ │
│  │    │ if (userPrompt == "history") return HistoryResponse()    │  │ │
│  │    │ if (userPrompt == "reset") return ResetResponse()        │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 5: Determine Query Type                             │  │ │
│  │    │ • Waiting for clarification answer?                      │  │ │
│  │    │   → memory.ResolveClarification(userPrompt)              │  │ │
│  │    │ • Follow-up query?                                        │  │ │
│  │    │   → memory.EnrichWithContext(userPrompt)                 │  │ │
│  │    │ • Continuous context?                                     │  │ │
│  │    │   → memory.BuildRefinedQuery(userPrompt)                 │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 6: Intent Analysis                                  │  │ │
│  │    │ var vagueness = IntentAnalyzer.Analyze(refinedPrompt)    │  │ │
│  │    │ if (vagueness.IsVague)                                   │  │ │
│  │    │   return ClarificationResponse(vagueness, memory)        │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 7: Semantic Cache Check                             │  │ │
│  │    │ var cached = await cacheService.GetCacheSearchAsync()    │  │ │
│  │    │ if (cached != null && cached.Similarity > 0.85)          │  │ │
│  │    │   return CachedSqlResponse(cached.SQL, memory)           │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 8: Vector Search (Schema Chunks)                    │  │ │
│  │    │ var output = await qdrantService.SearchAsync(            │  │ │
│  │    │   refinedPrompt,                                         │  │ │
│  │    │   embeddingService                                       │  │ │
│  │    │ )                                                         │  │ │
│  │    │ // Returns: top-5 relevant schema chunks + dependencies  │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 9: LLM Call (Azure OpenAI)                          │  │ │
│  │    │ var llmResult = await llmService.AskAsync(               │  │ │
│  │    │   output.Schema,          // Relevant chunks only        │  │ │
│  │    │   refinedPrompt,                                         │  │ │
│  │    │   memory.GetLatestTurn()  // Previous context            │  │ │
│  │    │ )                                                         │  │ │
│  │    │ // Returns: Generated SQL string                         │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 10: SQL Validation (4-Layer Pipeline)               │  │ │
│  │    │ var validationResult = await validator.ValidateAsync()   │  │ │
│  │    │ if (!validationResult.IsValid) {                         │  │ │
│  │    │   // Retry with fix hint (up to 3 times)                 │  │ │
│  │    │   llmResult = await llmService.FixSQL(                   │  │ │
│  │    │     llmResult,                                           │  │ │
│  │    │     validationResult.FixHint                             │  │ │
│  │    │   )                                                       │  │ │
│  │    │   validationResult = await validator.ValidateAsync()     │  │ │
│  │    │ }                                                         │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 11: Save to Semantic Cache                          │  │ │
│  │    │ await cacheService.SaveToCacheAsync(                     │  │ │
│  │    │   query: refinedPrompt,                                  │  │ │
│  │    │   sql: llmResult,                                        │  │ │
│  │    │   embedding: queryEmbedding                              │  │ │
│  │    │ )                                                         │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 12: Update Conversation Memory                      │  │ │
│  │    │ memory.UpdateContext(                                    │  │ │
│  │    │   userInput: userPrompt,                                 │  │ │
│  │    │   refinedQuery: refinedPrompt,                           │  │ │
│  │    │   generatedSQL: llmResult,                               │  │ │
│  │    │   entity: extractedEntity,                               │  │ │
│  │    │   entityName: resolvedName                               │  │ │
│  │    │ )                                                         │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │                        ↓                                          │ │
│  │    ┌──────────────────────────────────────────────────────────┐  │ │
│  │    │ STEP 13: Return Response                                 │  │ │
│  │    │ return new LLMResponse {                                 │  │ │
│  │    │   SQL = llmResult,                                       │  │ │
│  │    │   Type = ResponseType.SQL,                               │  │ │
│  │    │   Message = null                                         │  │ │
│  │    │ }                                                         │  │ │
│  │    └──────────────────────────────────────────────────────────┘  │ │
│  │  }                                                                │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

## 5.4 SqlValidator Layer (4-Layer Defense)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   SQL VALIDATOR LAYER (4-Layer Defense)                │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  SqlValidationPipeline.ValidateAsync(sql, currentTurn)            │ │
│  │                                                                    │ │
│  │  var pipeline = new PipelineResult { SQL = sql };                │ │
│  │                                                                    │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ LAYER 1: SyntaxValidator (<10ms)                           │  │ │
│  │  ├────────────────────────────────────────────────────────────┤  │ │
│  │  │ Checks (Regex-based):                                      │  │ │
│  │  │ • SELECT clause present?                                   │  │ │
│  │  │ • FROM clause present?                                     │  │ │
│  │  │ • Balanced parentheses?                                    │  │ │
│  │  │ • Closed string literals (even number of quotes)?         │  │ │
│  │  │ • JOIN has ON clause?                                      │  │ │
│  │  │ • SELECT * (warning only)?                                 │  │ │
│  │  │                                                            │  │ │
│  │  │ Result:                                                     │  │ │
│  │  │ • ValidationResult with structured errors                  │  │ │
│  │  │ • Error codes: SYN001-SYN006                               │  │ │
│  │  │ • If errors found: STOP pipeline, return fix hint          │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │                        ↓ (PASS)                                   │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ LAYER 2: SchemaValidator (<30ms)                           │  │ │
│  │  ├────────────────────────────────────────────────────────────┤  │ │
│  │  │ Checks (Schema-aware):                                     │  │ │
│  │  │ • Build alias map (FROM/JOIN table AS alias)               │  │ │
│  │  │ • All tables exist in schema?                              │  │ │
│  │  │   → If not: Levenshtein distance for "Did you mean?"      │  │ │
│  │  │ • All columns exist in referenced tables?                  │  │ │
│  │  │   → If not: Check if column exists in other tables         │  │ │
│  │  │   → Suggest: "Add JOIN TableX ON..." if FK relationship    │  │ │
│  │  │ • JOIN ON keys valid?                                      │  │ │
│  │  │ • WHERE clause columns valid?                              │  │ │
│  │  │ • ORDER BY columns valid?                                  │  │ │
│  │  │                                                            │  │ │
│  │  │ Result:                                                     │  │ │
│  │  │ • Error codes: SCH001-SCH005                               │  │ │
│  │  │ • Intelligent suggestions with FK relationship context     │  │ │
│  │  │ • If errors found: STOP pipeline, return fix hint          │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │                        ↓ (PASS)                                   │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ LAYER 3: LLMSelfCheckService (<2s) [OPTIONAL/DISABLED]     │  │ │
│  │  ├────────────────────────────────────────────────────────────┤  │ │
│  │  │ Checks (Semantic):                                         │  │ │
│  │  │ • Call Anthropic Claude Sonnet 4 API                       │  │ │
│  │  │ • Prompt: "Review this SQL against schema. Identify:       │  │ │
│  │  │   1. Tables referenced but not joined                      │  │ │
│  │  │   2. Ambiguous columns (same name in multiple tables)      │  │ │
│  │  │   3. Wrong JOIN conditions (incorrect FK usage)            │  │ │
│  │  │   4. Missing WHERE filters                                 │  │ │
│  │  │   5. Aggregations without GROUP BY                         │  │ │
│  │  │   6. Semantic mismatches"                                  │  │ │
│  │  │ • Parse JSON response: [{severity, code, message, fix}]   │  │ │
│  │  │                                                            │  │ │
│  │  │ Result:                                                     │  │ │
│  │  │ • Error codes: SEM001-SEM999 (from LLM)                    │  │ │
│  │  │ • Non-blocking: API failure → Log warning, continue        │  │ │
│  │  │ • If errors found: STOP pipeline, return fix hint          │  │ │
│  │  │                                                            │  │ │
│  │  │ [NOTE: Currently commented out in production]              │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │                        ↓ (PASS or SKIPPED)                        │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ LAYER 4: DryRunValidator (<200ms) [OPTIONAL]               │  │ │
│  │  ├────────────────────────────────────────────────────────────┤  │ │
│  │  │ Checks (Database runtime):                                 │  │ │
│  │  │ • Open SQL Server connection                               │  │ │
│  │  │ • SET SHOWPLAN_ALL ON (returns execution plan only)        │  │ │
│  │  │ • ExecuteReader(sql) → Generates plan WITHOUT data access  │  │ │
│  │  │ • Parse execution plan for:                                │  │ │
│  │  │   - Table scans (missing indexes)                          │  │ │
│  │  │   - Performance warnings                                   │  │ │
│  │  │ • SET SHOWPLAN_ALL OFF                                     │  │ │
│  │  │ • Close connection                                         │  │ │
│  │  │                                                            │  │ │
│  │  │ SQL Server Validation:                                     │  │ │
│  │  │ • Catch SqlException during plan generation                │  │ │
│  │  │ • Map error numbers to user-friendly messages:             │  │ │
│  │  │   - 208: Table/view does not exist                         │  │ │
│  │  │   - 207: Invalid column name                               │  │ │
│  │  │   - 4104: Multi-part identifier could not be bound         │  │ │
│  │  │   - 156: Incorrect syntax near keyword                     │  │ │
│  │  │                                                            │  │ │
│  │  │ Result:                                                     │  │ │
│  │  │ • Error codes: DRY001-DRY999                               │  │ │
│  │  │ • Non-blocking: Connection failure → Log warning, PASS     │  │ │
│  │  │ • If SQL errors found: STOP pipeline, return fix hint      │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  │                        ↓ (ALL LAYERS PASSED)                      │ │
│  │  ┌────────────────────────────────────────────────────────────┐  │ │
│  │  │ SUCCESS: SQL is VALID                                      │  │ │
│  │  │                                                            │  │ │
│  │  │ return new PipelineResult {                                │  │ │
│  │  │   SQL = sql,                                               │  │ │
│  │  │   FinalState = TurnState.RefinedQuery,                     │  │ │
│  │  │   FailedLayer = null,                                      │  │ │
│  │  │   FixHint = null,                                          │  │ │
│  │  │   IsValid = true,                                          │  │ │
│  │  │   LayerResults = { /* All layer results */ }               │  │ │
│  │  │ }                                                           │  │ │
│  │  └────────────────────────────────────────────────────────────┘  │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Error Recovery (Auto-Fix Loop)                                   │ │
│  │                                                                    │ │
│  │  if (!validationResult.IsValid && retryCount < 3) {               │ │
│  │    // Feed fix hint back to LLM                                   │ │
│  │    var fixedSQL = await llmService.FixSQL(                        │ │
│  │      originalSQL: sql,                                            │ │
│  │      fixHint: validationResult.FixHint                            │ │
│  │    );                                                              │ │
│  │                                                                    │ │
│  │    // Re-validate fixed SQL                                       │ │
│  │    validationResult = await ValidateAsync(fixedSQL, turn);        │ │
│  │  }                                                                 │ │
│  │                                                                    │ │
│  │  [Auto-Fix Success Rate: 82%]                                     │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

---

# 6. Data Flow Architecture

## 6.1 Complete Request-Response Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              DATA FLOW DIAGRAM                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

USER INPUT: "Show attendance for student10"
     │
     ▼
┌────────────────────────────────────┐
│  Widget (React State)              │
│  {                                 │
│    query: "Show attendance...",    │
│    loading: true,                  │
│    results: null,                  │
│    error: null                     │
│  }                                 │
└────────────┬───────────────────────┘
             │ HTTP POST
             ▼
┌────────────────────────────────────┐
│  API Request                       │
│  POST /api/query/natural           │
│  Headers: {                        │
│    X-Tenant-ID: "demo",            │
│    Content-Type: "application/json"│
│  }                                 │
│  Body: {                           │
│    query: "Show attendance..."     │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Middleware Processing             │
│  1. TenantContext {                │
│       TenantId: "demo",            │
│       SchemaFile: "DemoSchema.txt",│
│       ConnectionString: "...",     │
│       Quotas: { ... }              │
│     }                              │
│  2. Rate limit check: 45/100      │
│  3. Quota check: 23K/50K tokens    │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  LLMService Input                  │
│  ProcessQuery(                     │
│    userPrompt: "Show attendance...",│
│    tenant: {                       │
│      TenantId: "demo",             │
│      SchemaFile: "DemoSchema.txt", │
│      MappingFile: "mapping.json"   │
│    }                               │
│  )                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Schema Loading                    │
│  FullSchema = ReadFile(            │
│    "./SchemaFiles/DemoSchema.txt"  │
│  )                                 │
│  → 30 tables, 200+ columns         │
│  → 45 foreign keys                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  STM Context Check                 │
│  LatestTurn = {                    │
│    TurnNumber: 4,                  │
│    UserInput: "Show fee...",       │
│    Entity: "student9",             │
│    GeneratedSQL: "SELECT..."       │
│  }                                 │
│  → Not duplicate query             │
│  → Not follow-up (no signals)      │
│  → Not clarification response      │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Intent Analysis                   │
│  Analyze("Show attendance...") {   │
│    VagueTerms: [],                 │
│    SpecificTerms: ["attendance"],  │
│    IsVague: false,                 │
│    ConfidenceScore: 0.95           │
│  }                                 │
│  → No clarification needed         │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Semantic Cache Lookup             │
│  QueryEmbedding = [0.123, -0.456,  │
│    ..., 0.789] (1536 dims)         │
│  CacheSearch(embedding) {          │
│    TopMatch: similarity = 0.73     │
│    Threshold: 0.85                 │
│    Result: MISS                    │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Qdrant Vector Search              │
│  QueryEmbedding → Qdrant           │
│  SearchResults = [                 │
│    {                               │
│      table: "Students",            │
│      score: 0.92,                  │
│      chunk: "Table: Students..."   │
│    },                              │
│    {                               │
│      table: "Attendance",          │
│      score: 0.89,                  │
│      chunk: "Table: Attendance..." │
│    },                              │
│    ... (top-5 chunks)              │
│  ]                                 │
│  → Resolve FK: Students.StudentId  │
│    ↔ Attendance.StudentId          │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  LLM Prompt Construction           │
│  Prompt = """                      │
│  You are an expert SQL architect.  │
│                                    │
│  SCHEMA:                           │
│  Table: Students                   │
│    - StudentId (VARCHAR) PK        │
│    - Name (VARCHAR)                │
│  Table: Attendance                 │
│    - AttendanceId (INT) PK         │
│    - StudentId (VARCHAR) FK        │
│    - Date (DATE)                   │
│    - Status (VARCHAR)              │
│  FK: Attendance.StudentId →        │
│      Students.StudentId            │
│                                    │
│  ENUM Context:                     │
│  Attendance.Status = ['Present',   │
│    'Absent', 'Late']               │
│                                    │
│  RULES:                            │
│  • Use T-SQL syntax                │
│  • Generate only SELECT queries    │
│  • Use exact enum values           │
│                                    │
│  USER QUESTION:                    │
│  Show attendance for student10     │
│                                    │
│  Generate ONLY the SQL query.      │
│  """                               │
│                                    │
│  TokenCount = 3,250 tokens         │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Azure OpenAI API Call             │
│  Request: {                        │
│    model: "gpt-4o",                │
│    messages: [{                    │
│      role: "user",                 │
│      content: "<prompt>"           │
│    }],                             │
│    temperature: 0.0,               │
│    max_tokens: 500                 │
│  }                                 │
│  Response: {                       │
│    choices: [{                     │
│      message: {                    │
│        content: "SELECT s.Name,    │
│          a.Date, a.Status          │
│          FROM Students s           │
│          INNER JOIN Attendance a   │
│            ON s.StudentId =        │
│            a.StudentId             │
│          WHERE s.StudentId =       │
│            'student10'"            │
│      }                             │
│    }],                             │
│    usage: {                        │
│      prompt_tokens: 3250,          │
│      completion_tokens: 50,        │
│      total_tokens: 3300            │
│    }                               │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  SQL Validation Input              │
│  ValidateAsync(                    │
│    sql: "SELECT s.Name, a.Date,    │
│      a.Status FROM Students s      │
│      INNER JOIN Attendance a       │
│      ON s.StudentId = a.StudentId  │
│      WHERE s.StudentId = 'student10'",│
│    turn: { TurnNumber: 5 }         │
│  )                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Layer 1: Syntax Validation        │
│  SyntaxResult = {                  │
│    IsValid: true,                  │
│    Errors: [],                     │
│    Warnings: [                     │
│      {                             │
│        Code: "SYN006",             │
│        Message: "Consider specifying│
│          column aliases for clarity"│
│      }                             │
│    ]                               │
│  }                                 │
│  Time: 8ms                         │
└────────────┬───────────────────────┘
             │ PASS
             ▼
┌────────────────────────────────────┐
│  Layer 2: Schema Validation        │
│  AliasMap = {                      │
│    "Students": "s",                │
│    "Attendance": "a"               │
│  }                                 │
│  Checks:                           │
│  ✓ Table 'Students' exists         │
│  ✓ Table 'Attendance' exists       │
│  ✓ Column 's.Name' valid           │
│  ✓ Column 'a.Date' valid           │
│  ✓ Column 'a.Status' valid         │
│  ✓ JOIN key 's.StudentId' valid    │
│  ✓ JOIN key 'a.StudentId' valid    │
│  ✓ WHERE column 's.StudentId' valid│
│  SchemaResult = {                  │
│    IsValid: true,                  │
│    Errors: [],                     │
│    Warnings: []                    │
│  }                                 │
│  Time: 25ms                        │
└────────────┬───────────────────────┘
             │ PASS
             ▼
┌────────────────────────────────────┐
│  Layer 4: Dry-Run Validation       │
│  (Layer 3 skipped - disabled)      │
│  Connection: SQL Server            │
│  SET SHOWPLAN_ALL ON               │
│  ExecuteReader(sql)                │
│  → Execution plan generated        │
│  → No errors detected              │
│  → Performance: Index seek on PK   │
│  SET SHOWPLAN_ALL OFF              │
│  DryRunResult = {                  │
│    IsValid: true,                  │
│    Errors: [],                     │
│    Warnings: [],                   │
│    Info: ["Query plan OK"]         │
│  }                                 │
│  Time: 180ms                       │
└────────────┬───────────────────────┘
             │ PASS
             ▼
┌────────────────────────────────────┐
│  Validation Complete               │
│  PipelineResult = {                │
│    SQL: "SELECT...",               │
│    FinalState: RefinedQuery,       │
│    IsValid: true,                  │
│    FailedLayer: null,              │
│    FixHint: null,                  │
│    TotalTime: 213ms                │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Save to Semantic Cache            │
│  CacheRecord = {                   │
│    QueryText: "Show attendance...", │
│    QueryEmbedding: [0.123, ...],   │
│    ExecutableSQL: "SELECT...",     │
│    CreatedAt: "2026-05-30T10:30:00"│
│  }                                 │
│  → Stored in SQL Server            │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Update STM                        │
│  NewTurn = {                       │
│    TurnNumber: 5,                  │
│    UserInput: "Show attendance...",│
│    RefinedQuery: "Show attendance...",│
│    GeneratedSQL: "SELECT...",      │
│    Entity: "student10",            │
│    EntityName: "student10",        │
│    Timestamp: "2026-05-30T10:30:00"│
│  }                                 │
│  ConversationChain.Turns.Add(NewTurn)│
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  LLMService Response               │
│  LLMResponse = {                   │
│    SQL: "SELECT s.Name, a.Date,    │
│      a.Status FROM Students s      │
│      INNER JOIN Attendance a...",  │
│    Type: ResponseType.SQL,         │
│    Message: null,                  │
│    PromptTokens: 3250,             │
│    CompletionTokens: 50,           │
│    TotalTokens: 3300               │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  API: Execute SQL                  │
│  Connection: Tenant Database       │
│  ExecuteQuery(                     │
│    sql: "SELECT...",               │
│    connectionString: "Server=..."  │
│  )                                 │
│  Results = [                       │
│    {                               │
│      Name: "John Doe",             │
│      Date: "2026-05-29",           │
│      Status: "Present"             │
│    },                              │
│    {                               │
│      Name: "John Doe",             │
│      Date: "2026-05-28",           │
│      Status: "Absent"              │
│    },                              │
│    ... (10 rows)                   │
│  ]                                 │
│  ExecutionTime: 150ms              │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  API: Track Token Usage            │
│  TokenUsageRecord = {              │
│    TenantId: "demo",               │
│    ConversationId: "conv-123",     │
│    PromptTokens: 3250,             │
│    CompletionTokens: 50,           │
│    TotalTokens: 3300,              │
│    Cost: 0.099,                    │
│    Timestamp: "2026-05-30T10:30:00"│
│  }                                 │
│  → Stored in TokenUsage table      │
│  → Updated tenant daily aggregate  │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  API Response                      │
│  200 OK                            │
│  {                                 │
│    sql: "SELECT s.Name...",        │
│    results: [                      │
│      { Name: "John Doe", ... },    │
│      ...                           │
│    ],                              │
│    tokenUsage: {                   │
│      promptTokens: 3250,           │
│      completionTokens: 50,         │
│      totalTokens: 3300,            │
│      cost: 0.099                   │
│    },                              │
│    executionTime: 150,             │
│    cached: false                   │
│  }                                 │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Widget: Update State              │
│  setState({                        │
│    query: "Show attendance...",    │
│    loading: false,                 │
│    results: [...],                 │
│    error: null,                    │
│    tokenUsage: {                   │
│      totalTokens: 3300,            │
│      cost: 0.099                   │
│    }                               │
│  })                                │
└────────────┬───────────────────────┘
             │
             ▼
┌────────────────────────────────────┐
│  Widget: Render UI                 │
│  • <ResultsTable> with 10 rows     │
│  • <TokenUsageBadge> shows 3.3K    │
│  • <ConversationHistory> updated   │
│  • <QueryInput> ready for next query│
└────────────┬───────────────────────┘
             │
             ▼
         USER SEES RESULTS

[Total Time Breakdown]
  Widget → API: 5ms
  Middleware: 5ms
  LLM Processing: 3,200ms
    - Schema load: 10ms
    - STM check: 5ms
    - Intent analysis: 5ms
    - Cache check: 100ms (miss)
    - Vector search: 150ms
    - LLM call: 2,800ms
    - Validation: 213ms
    - Save cache: 50ms
    - Update STM: 17ms
  SQL Execution: 150ms
  Token tracking: 20ms
  API → Widget: 5ms
  Widget render: 50ms
  ────────────────────────
  TOTAL: ~5s
```

---

**END OF DOCUMENT**

This comprehensive architecture document provides complete visibility into:
1. **System Architecture** - 4-layer stack with internal components
2. **Sequence Diagrams** - Happy path, error recovery, and clarification flows
3. **Layer-by-Layer Architecture** - Detailed breakdown of Widget, API, LLMService, and SqlValidator
4. **Data Flow** - Complete request-response flow with all data transformations

The diagrams show the complete linkage and internal functionality of all system layers, making it easy to understand how a user query flows through the entire stack from React widget to database execution.
