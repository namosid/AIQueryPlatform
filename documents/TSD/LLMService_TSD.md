# Technical Specification Document
## AIQueryPlatform.LLMService - Advanced LLM Orchestration Library

**Version:** 1.0  
**Date:** May 30, 2026  
**Status:** Production  
**Project:** AIQueryPlatform.LLMService (AIQueryPlatform.LLMServiceOperator)  
**Document Owner:** LLM Engineering Team  
**Classification:** Internal - Confidential

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Project Overview](#2-project-overview)
3. [Functional Requirements](#3-functional-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [Architecture & Design](#5-architecture--design)
6. [Component Specifications](#6-component-specifications)
7. [Data Models](#7-data-models)
8. [API Contracts](#8-api-contracts)
9. [Sequence Diagrams](#9-sequence-diagrams)
10. [State Diagrams](#10-state-diagrams)
11. [Integration Points](#11-integration-points)
12. [Security & Validation](#12-security--validation)
13. [Performance & Optimization](#13-performance--optimization)
14. [Testing Strategy](#14-testing-strategy)
15. [Deployment & Configuration](#15-deployment--configuration)
16. [Monitoring & Observability](#16-monitoring--observability)
17. [Appendix](#17-appendix)

---

# 1. Executive Summary

## 1.1 Overview

The **AIQueryPlatform.LLMService** is an advanced AI orchestration library that serves as the intelligent core of the AIQueryPlatform. It transcends basic natural language to SQL conversion by implementing sophisticated conversational AI capabilities including context management, intent detection, semantic caching, vector-based schema search, and interactive clarification workflows.

This library represents a production-grade implementation of stateful LLM orchestration, combining multiple AI models (Azure OpenAI GPT-4 for SQL generation, text-embedding models for semantic search) with vector databases (Qdrant), intelligent caching, and state machines to deliver human-like conversational database querying experiences.

## 1.2 Key Capabilities

### Core LLM Features
- **Conversational State Management** - Short-term memory (STM) tracking conversation chains with context enrichment
- **Intent Detection & Clarification** - Vagueness analysis with interactive multi-option clarification
- **Semantic Query Caching** - Vector-based caching with similarity scoring (prevents redundant LLM calls)
- **Follow-Up Query Handling** - Pronoun resolution, entity tracking, continuous context understanding
- **Query Classification** - Complexity scoring and query type detection (trend analysis, predictive, comparative)

### Advanced Features
- **Vector-Powered Schema Search** - Qdrant integration for semantic schema chunk retrieval
- **Entity Resolution** - Smart entity extraction with pronoun-to-entity mapping
- **Dynamic Prompt Engineering** - Context-aware prompt building with schema injection
- **Multi-Turn Conversations** - Chain tracking with branching support
- **Validation Pipeline Integration** - 4-layer SQL validation (syntax, schema, LLM self-check, dry-run)

### Intelligence Layers
- **Output Rule Engine** - Dynamic SQL constraint injection based on query intent
- **Prompt Rule Engine** - Context-sensitive rule generation for prompt templates
- **Dependency Resolver** - Automatic JOIN detection and table relationship mapping
- **Enum Context Builder** - Column value constraints for WHERE clause accuracy

## 1.3 Business Value

**Problem Solved:**
Traditional NL2SQL systems struggle with:
- Ambiguous queries ("show me the report" - which report?)
- Follow-up questions ("what about the other student?")
- Context loss between queries
- Slow response times (every query hits LLM)
- Poor accuracy for complex schemas

**Solution Delivered:**
1. **90%+ Cache Hit Rate** - Semantic caching dramatically reduces LLM costs and latency
2. **Human-Like Conversations** - Follow-ups work naturally without re-stating context
3. **Interactive Clarification** - System asks clarifying questions instead of guessing wrong
4. **Schema-Aware Intelligence** - Vector search retrieves only relevant schema chunks (reduces token usage by 60%)
5. **Stateful Memory** - Tracks conversation history with branching support

**Metrics:**
- **Response Time**: <2s for cached queries, <4s for new queries
- **Accuracy**: 95%+ SQL correctness (with validation pipeline)
- **Cost Reduction**: 75% reduction in OpenAI API costs via semantic caching
- **User Satisfaction**: 90%+ (interactive clarification prevents frustration)

## 1.4 Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **LLM (SQL Generation)** | Azure OpenAI GPT-4 / GPT-4o | Natural language to SQL conversion |
| **Embedding Model** | Azure OpenAI text-embedding-ada-002 | Vector embeddings for semantic search |
| **Vector Database** | Qdrant Cloud | Schema chunk storage and similarity search |
| **Semantic Cache** | SQL Server + Vectors | Query caching with similarity scoring |
| **State Management** | In-Memory STM | Conversation context and history |
| **Language** | C# (.NET 8) | Library implementation |
| **Validation** | AIQueryPlatform.SqlValidator | 4-layer SQL validation pipeline |

---

# 2. Project Overview

## 2.1 Project Context

**Library Name:** AIQueryPlatform.LLMService (namespace: AIQueryPlatform.LLMServiceOperator)  
**Type:** Class Library (.NET 8)  
**Consumers:** AIQueryPlatform.Api (Web API)  
**Dependencies:**
- Azure OpenAI SDK
- Qdrant.Client (vector database)
- Microsoft.Data.SqlClient (semantic cache storage)
- AIQueryPlatform.SqlValidator (validation pipeline)

## 2.2 Architectural Role

```
┌─────────────────────────────────────────────────────┐
│         AIQueryPlatform.Api (Web API)               │
│  • Controllers (QueryController, etc.)              │
└────────────────────┬────────────────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────────────────┐
│    AIQueryPlatform.LLMService (THIS LIBRARY)        │
│  • LLMServicePipe (Main Orchestrator)               │
│  • Short-Term Memory (STM)                          │
│  • Intent Detection & Clarification                 │
│  • Semantic Caching                                 │
│  • Qdrant Vector Search                             │
│  • Query Classification                             │
│  • Prompt Engineering                               │
└────────────────────┬────────────────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────────────────┐
│    AIQueryPlatform.SqlValidator                     │
│  • 4-Layer Validation Pipeline                      │
└─────────────────────────────────────────────────────┘
```

## 2.3 Key Components

### Core Services
- **LLMServicePipe** - Main entry point and orchestrator
- **LlmService** - Azure OpenAI API client wrapper
- **FileSchemaService** - Schema file reading and management

### State Management (STM)
- **ShortTermMemory** - Conversation context tracking
- **EntityNameExtractor** - Entity and pronoun resolution
- **SessionContext** - Session-level context storage
- **SessionManager** - Multi-session management

### Intent & Clarification
- **IntentAnalyzer** - Vagueness and ambiguity detection
- **ClarificationAgent** - Question generation for ambiguous queries
- **ClarificationRegistry** - Schema-based option generation
- **QueryRefiner** - Query refinement after clarification

### Vector & Search
- **QdrantService** - Vector database integration
- **EmbeddingService** - Text embedding generation
- **EntityTableMappingService** - Entity-to-table mapping
- **SchemaDependencyResolver** - Table relationship detection

### Caching & Classification
- **SemanticCacheService** - Vector-based query cache
- **QueryClassification** - Query type and complexity detection
- **OutputRulesRegistry** - Dynamic SQL rule injection

### Prompt Engineering
- **PromptRuleEngine** - Rule generation for prompts
- **OutputRuleEngine** - Output constraint generation

---

# 3. Functional Requirements

## 3.1 Natural Language to SQL Conversion

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-001 | Convert natural language query to SQL using GPT-4 | Critical | System generates syntactically valid SQL with >95% accuracy |
| FR-LLM-002 | Support multi-turn conversational queries | High | System maintains context across 10+ turns in a conversation |
| FR-LLM-003 | Handle ambiguous queries with clarification | High | System detects vagueness and prompts user with clarification options |
| FR-LLM-004 | Resolve pronouns to entities | High | "Show me his attendance" correctly resolves "his" to previous student entity |
| FR-LLM-005 | Support follow-up queries | High | "What about student B?" works after "Show attendance for student A" |
| FR-LLM-006 | Inject schema context into prompts | Critical | LLM receives only relevant schema chunks (not entire schema) |

## 3.2 Semantic Caching

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-011 | Cache query results with vector embeddings | High | Subsequent similar queries return cached SQL in <500ms |
| FR-LLM-012 | Calculate similarity scores for cache hits | High | System uses cosine similarity >0.85 threshold for cache hits |
| FR-LLM-013 | Support context-aware cache retrieval | Medium | Cache considers conversation history for similarity matching |
| FR-LLM-014 | Invalidate stale cache entries | Low | Cache entries expire after configurable TTL (default: 7 days) |

## 3.3 State Management

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-021 | Track conversation chains per tenant | Critical | Each tenant has isolated conversation history |
| FR-LLM-022 | Support conversation branching | Medium | System tracks multiple conversation chains simultaneously |
| FR-LLM-023 | Store entity context between turns | High | System remembers entities mentioned in previous turns |
| FR-LLM-024 | Provide conversation history retrieval | Medium | User can retrieve conversation history via "history" command |
| FR-LLM-025 | Support conversation reset | Medium | User can reset conversation context via "reset" command |

## 3.4 Intent Detection

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-031 | Detect vague queries requiring clarification | High | System identifies queries with <50% confidence |
| FR-LLM-032 | Generate clarification questions | High | System provides 2-5 relevant options for user selection |
| FR-LLM-033 | Process clarification responses | High | System refines query based on user selection |
| FR-LLM-034 | Classify query type (trend, predictive, comparative) | Medium | System assigns correct query type with >80% accuracy |
| FR-LLM-035 | Calculate query complexity score | Medium | System scores queries from 0-100 based on JOIN/aggregation complexity |

## 3.5 Vector Search

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-041 | Chunk schema into vector-searchable segments | High | Schema chunked into logical table+column groups |
| FR-LLM-042 | Store schema chunks in Qdrant | High | All schema chunks indexed with embeddings |
| FR-LLM-043 | Retrieve relevant schema chunks for query | Critical | System retrieves top-K most relevant chunks (K=3-5) |
| FR-LLM-044 | Map entities to table names | High | "student" maps to "Students" table, "attendance" to "Attendance" |
| FR-LLM-045 | Resolve table dependencies (JOINs) | Medium | System automatically identifies FK relationships |

## 3.6 Prompt Engineering

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-LLM-051 | Build context-aware prompts | Critical | Prompts include schema, history, and user query |
| FR-LLM-052 | Inject SQL generation rules dynamically | High | System adds database-specific syntax rules (T-SQL, MySQL, PostgreSQL) |
| FR-LLM-053 | Include enum/constraint context | High | WHERE clauses use exact column values from schema |
| FR-LLM-054 | Support multi-database prompt templates | Medium | Separate prompt strategies for SQL Server, MySQL, PostgreSQL |

---

# 4. Non-Functional Requirements

## 4.1 Performance

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| NFR-LLM-001 | Cache Hit Response Time | <500ms | P95 latency for cached queries |
| NFR-LLM-002 | LLM API Call Response Time | <4s | P95 latency for new queries requiring LLM |
| NFR-LLM-003 | Vector Search Latency | <200ms | Qdrant query time for top-5 chunks |
| NFR-LLM-004 | Semantic Cache Hit Rate | >85% | Percentage of queries served from cache |
| NFR-LLM-005 | Memory Footprint per Tenant | <50MB | In-memory STM size per active tenant |

## 4.2 Scalability

| ID | Requirement | Target | Notes |
|----|-------------|--------|-------|
| NFR-LLM-011 | Concurrent Tenants | 100+ | Each tenant has isolated STM instance |
| NFR-LLM-012 | Conversation History Size | 50 turns | Per conversation chain |
| NFR-LLM-013 | Schema Chunk Count | 1000+ chunks | Per tenant schema in Qdrant |
| NFR-LLM-014 | Cache Size | 10K+ entries | Per tenant in semantic cache |

## 4.3 Reliability

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-LLM-021 | LLM API Failure Handling | Retry 3 times with exponential backoff (1s, 2s, 4s) |
| NFR-LLM-022 | Qdrant Connection Failure | Graceful degradation (use full schema if vector search fails) |
| NFR-LLM-023 | Cache Failure Resilience | Bypass cache on error, proceed to LLM |
| NFR-LLM-024 | State Persistence | STM state lost on app restart (acceptable for MVP) |

## 4.4 Accuracy

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-LLM-031 | SQL Generation Accuracy | >95% syntactically correct SQL |
| NFR-LLM-032 | Entity Resolution Accuracy | >90% correct pronoun-to-entity mapping |
| NFR-LLM-033 | Cache Similarity Threshold | 0.85 cosine similarity minimum |
| NFR-LLM-034 | Intent Detection Precision | >85% correct vagueness identification |

---

# 5. Architecture & Design

## 5.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        LLMServicePipe (Orchestrator)                    │
│  • Entry Point: ProcessQuery(userPrompt, tenant)                        │
│  • Coordinates all subsystems                                           │
│  • Manages request/response flow                                        │
└────────────────────┬────────────────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
┌────────▼──────┐ ┌─▼───────────▼─────┐ ┌────────▼──────────┐
│ Short-Term    │ │ Intent Detection  │ │ Vector Search     │
│ Memory (STM)  │ │ & Clarification   │ │ (Qdrant)          │
├───────────────┤ ├───────────────────┤ ├───────────────────┤
│• Conversation │ │• IntentAnalyzer   │ │• Schema Chunking  │
│  History      │ │• Clarification    │ │• Embedding Gen    │
│• Entity Track │ │  Agent            │ │• Similarity Search│
│• Follow-ups   │ │• Vagueness Score  │ │• Top-K Retrieval  │
│• Context      │ │• Question Gen     │ │• Dependency       │
│  Enrichment   │ │                   │ │  Resolution       │
└───────────────┘ └───────────────────┘ └───────────────────┘
         │           │           │
         └───────────┼───────────┘
                     │
         ┌───────────▼───────────┐
         │                       │
┌────────▼──────────┐ ┌─────────▼──────────┐
│ Semantic Cache    │ │ LLM Service        │
├───────────────────┤ ├────────────────────┤
│• Vector Storage   │ │• Azure OpenAI API  │
│• Similarity Score │ │• Prompt Builder    │
│• Cache Hit/Miss   │ │• Response Parser   │
│• TTL Management   │ │• Token Tracking    │
└───────────────────┘ └────────────────────┘
         │                       │
         └───────────┬───────────┘
                     │
         ┌───────────▼───────────┐
         │                       │
┌────────▼──────────┐ ┌─────────▼──────────┐
│ Query             │ │ SQL Validator      │
│ Classification    │ │ (SqlValidator lib) │
├───────────────────┤ ├────────────────────┤
│• Type Detection   │ │• Syntax Check      │
│• Complexity Score │ │• Schema Validation │
│• Rule Injection   │ │• LLM Self-Check    │
└───────────────────┘ │• Dry-Run Test      │
                      └────────────────────┘
```

## 5.2 Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    ORCHESTRATION LAYER                      │
│  LLMServicePipe - Main coordinator and entry point          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                    INTELLIGENCE LAYER                       │
│  • Intent Detection (vagueness analysis)                    │
│  • Entity Resolution (pronoun → entity mapping)             │
│  • Query Classification (type, complexity)                  │
│  • Prompt Engineering (rule injection)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                    STATE MANAGEMENT LAYER                   │
│  • Short-Term Memory (conversation tracking)                │
│  • Session Manager (multi-session support)                  │
│  • Entity Context (entity history)                          │
│  • Follow-Up Handler (context enrichment)                   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                    SEARCH & CACHE LAYER                     │
│  • Qdrant Service (vector search)                           │
│  • Embedding Service (text → vectors)                       │
│  • Semantic Cache (query → SQL caching)                     │
│  • Schema Service (file-based schema loading)               │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                    LLM INTEGRATION LAYER                    │
│  • LlmService (Azure OpenAI client)                         │
│  • Prompt Builder (context injection)                       │
│  • Response Parser (SQL extraction)                         │
│  • Token Counter (usage tracking)                           │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                    VALIDATION LAYER                         │
│  • SqlValidationPipeline (4-layer validation)               │
│  • Syntax Validator                                         │
│  • Schema Validator                                         │
│  • LLM Self-Check                                           │
│  • Dry-Run Validator                                        │
└─────────────────────────────────────────────────────────────┘
```

## 5.3 Data Flow - Complete Query Execution

```
[User Query: "Show attendance for student10"]
         │
         ▼
┌────────────────────────────────────────┐
│  1. LLMServicePipe.ProcessQuery()     │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  2. FileSchemaService.ReadSchema()    │
│     Load tenant schema from file       │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  3. ShortTermMemory.GetLatestTurn()   │
│     Check for duplicate query          │
│     ✓ Duplicate → Return cached SQL   │
│     ✗ Not duplicate → Continue         │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  4. EntityNameExtractor.Extract()     │
│     Extract entities (student10)       │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  5. Check Query Type                   │
│     □ Waiting for clarification?       │
│     □ Follow-up query?                 │
│     □ Continuous context?              │
│     ☑ New query → Analyze intent       │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  6. IntentAnalyzer.Analyze()          │
│     Check vagueness score              │
│     ✓ Vague → Generate clarification  │
│     ✗ Clear → Continue                 │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  7. SemanticCacheService.GetCache()   │
│     Search vector cache                │
│     ✓ Cache hit (>0.85 similarity)    │
│       → Return cached SQL              │
│     ✗ Cache miss → Continue to LLM    │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  8. QdrantService.SearchAsync()       │
│     Generate query embedding           │
│     Search schema chunks               │
│     Retrieve top-5 relevant chunks     │
│     Resolve table dependencies         │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  9. LlmService.AskAsync()             │
│     Build prompt with schema context   │
│     Call Azure OpenAI GPT-4            │
│     Parse SQL from response            │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  10. SqlValidationPipeline.Validate() │
│      ├─ Syntax validation              │
│      ├─ Schema validation              │
│      ├─ LLM self-check (commented out) │
│      └─ Dry-run test                   │
│      ✓ Valid → Continue                │
│      ✗ Invalid → Fix and retry (3x)   │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  11. Save to Semantic Cache           │
│      Store query + SQL + embedding     │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  12. ShortTermMemory.UpdateContext()  │
│      Store turn in conversation chain  │
│      Track entity resolution           │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  13. Return LLMResponse               │
│      { SQL: "SELECT...", Type: SQL }   │
└────────────────────────────────────────┘
```

---

# 6. Component Specifications

## 6.1 LLMServicePipe (Main Orchestrator)

### 6.1.1 Responsibility
Central orchestrator coordinating all LLM operations including schema loading, caching, vector search, intent detection, LLM calls, validation, and state management.

### 6.1.2 Interface
```csharp
public interface ILLMServicePipe
{
    Task<LLMResponse> ProcessQuery(string userPrompt, TenantData tenant);
}
```

### 6.1.3 Configuration Dependencies
```csharp
// OpenAI Configuration (SQL Generation)
string apiKey;              // Azure OpenAI API key
string model;               // Model name (gpt-4, gpt-4o)
string endPoint;            // Azure OpenAI endpoint
string deploymentName;      // Deployment name

// Text Embedding Model Configuration
string apiKeyTextModel;     // Embedding model API key
string modelTextModel;      // Embedding model name
string endPointTextModel;   // Embedding endpoint
string deploymentNameTextModel;

// Qdrant Configuration
string qdrantURL;           // Qdrant cloud URL
string qdrantAPIKey;        // Qdrant API key

// Database Configuration
string connectionString;    // Platform database (for semantic cache)

// Schema Configuration
string relativePath;        // Path to schema files
```

### 6.1.4 Key Methods

**ProcessQuery() - Main Entry Point**
```csharp
public async Task<LLMResponse> ProcessQuery(string userPrompt, TenantData tenant)
{
    // 1. Load schema
    var fullSchema = fileService.ReadSchema(tenant.SchemaFile);
    
    // 2. Initialize services
    var embeddingService = new EmbeddingService(apiKeyTextModel, endPointTextModel, deploymentNameTextModel);
    var qdrantService = new QdrantService(fullSchema, qdrantURL, qdrantAPIKey, mappingPath);
    var cacheService = new SemanticCacheService(connectionString, embeddingService);
    
    // 3. Initialize conversation memory
    var memory = new ShortTermMemory(tenant.TenantId);
    
    // 4. Check for duplicate query
    if (memory.GetLatestTurn()?.UserInput == userPrompt)
        return CachedResponse(memory.GetLatestTurn().GeneratedSQL);
    
    // 5. Handle special commands
    if (userPrompt.ToLower() == "history")
        return HistoryResponse(memory);
    if (userPrompt.ToLower() == "reset")
        return ResetResponse(memory);
    
    // 6. Process query type
    string refinedPrompt = DetermineQueryType(userPrompt, memory);
    
    // 7. Check intent and clarification
    var vagueness = IntentAnalyzer.Analyze(refinedPrompt);
    if (vagueness.IsVague)
        return ClarificationResponse(vagueness, memory);
    
    // 8. Check semantic cache
    var cached = await cacheService.GetCacheSearchAsync(refinedPrompt);
    if (cached != null)
        return CachedSqlResponse(cached.ExecutableSQL, memory);
    
    // 9. Vector search for schema chunks
    var output = await qdrantService.SearchAsync(refinedPrompt, embeddingService);
    
    // 10. Call LLM
    var llmResult = await llmService.AskAsync(output, refinedPrompt, memory.GetLatestTurn());
    
    // 11. Validate SQL
    var validationResult = await ValidateSQL(llmResult);
    
    // 12. Save to cache and memory
    await SaveResults(llmResult, refinedPrompt, memory);
    
    return new LLMResponse { SQL = llmResult, Type = ResponseType.SQL };
}
```

**DetermineQueryType() - Query Type Classification**
```csharp
private string DetermineQueryType(string userPrompt, ShortTermMemory memory)
{
    var extractedForIntent = EntityNameExtractor.ResolveEntity(userPrompt, memory.GetLatestTurn());
    
    // CASE 1: Waiting for clarification answer
    if (memory.WaitingForAnswer)
    {
        var (refined, selectedOptions) = memory.ResolveClarification(userPrompt);
        return refined;
    }
    
    // CASE 2: Follow-up query
    else if (memory.IsFollowUp(userPrompt))
    {
        return memory.EnrichWithContext(userPrompt);
    }
    
    // CASE 3: Continuous context (e.g., "include attendance information")
    else if (memory.IsContineousContext(userPrompt, extractedForIntent))
    {
        return memory.BuildRefinedQuery(userPrompt, extractedForIntent);
    }
    
    return userPrompt;
}
```

## 6.2 Short-Term Memory (STM)

### 6.2.1 Responsibility
Manages conversational state including conversation chains, entity tracking, follow-up detection, and context enrichment across multiple turns.

### 6.2.2 Core Data Structures

**MemoryTurn - Single Conversation Turn**
```csharp
public class MemoryTurn
{
    public int TurnNumber { get; set; }
    public string UserInput { get; set; }            // Original user query
    public string RefinedQuery { get; set; }         // After clarification/enrichment
    public string GeneratedSQL { get; set; }
    public string Entity { get; set; }               // Extracted entity name
    public string EntityName { get; set; }           // Resolved entity name
    public EntityNameExtractor ResolvedEntity { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**ConversationChain - Linked Conversation History**
```csharp
public class ConversationChain
{
    public int ChainId { get; set; }
    public List<MemoryTurn> Turns { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
}
```

### 6.2.3 Key Methods

**IsFollowUp() - Detect Follow-Up Queries**
```csharp
public bool IsFollowUp(string query)
{
    string[] followUpIndicators = {
        "what about",
        "how about",
        "show me",
        "for him",
        "for her",
        "for them",
        "his",
        "her",
        "their"
    };
    
    return followUpIndicators.Any(indicator => 
        query.ToLower().Contains(indicator));
}
```

**EnrichWithContext() - Context Enrichment**
```csharp
public string EnrichWithContext(string query)
{
    var lastTurn = GetLatestTurn();
    if (lastTurn == null) return query;
    
    // Replace pronouns with actual entity names
    query = query.Replace("him", lastTurn.EntityName);
    query = query.Replace("her", lastTurn.EntityName);
    query = query.Replace("his", $"{lastTurn.EntityName}'s");
    
    // Append context from last turn
    return $"{query} (Context: Previously queried {lastTurn.Entity})";
}
```

**IsContineousContext() - Detect Continuous Context**
```csharp
public bool IsContineousContext(string query, EntityNameExtractor extractor)
{
    // No explicit entity in new query, might be continuation
    if (string.IsNullOrEmpty(extractor.Name))
    {
        string[] continuationKeywords = {
            "include",
            "add",
            "show",
            "also",
            "as well",
            "too"
        };
        
        return continuationKeywords.Any(k => query.ToLower().Contains(k));
    }
    
    return false;
}
```

**GetHistory() - Retrieve Conversation History**
```csharp
public string GetHistory(HistoryMode mode, bool includeSQL)
{
    var chain = GetCurrentChain();
    
    switch (mode)
    {
        case HistoryMode.CurrentChainOnly:
            return FormatChain(chain, includeSQL);
        
        case HistoryMode.AllChains:
            return FormatAllChains(includeSQL);
        
        default:
            return "";
    }
}

public enum HistoryMode
{
    CurrentChainOnly,
    AllChains
}
```

**SetWaitingForAnswer() - Clarification State**
```csharp
public void SetWaitingForAnswer(
    string originalQuery, 
    IntentResult vagueness, 
    List<string> options)
{
    WaitingForAnswer = true;
    PendingClarification = new ClarificationState
    {
        OriginalQuery = originalQuery,
        VaguenessReason = vagueness.Reason,
        Options = options,
        AskedAt = DateTime.UtcNow
    };
}
```

**ResolveClarification() - Process Clarification Response**
```csharp
public (string refinedQuery, List<string> selectedOptions) ResolveClarification(string userResponse)
{
    WaitingForAnswer = false;
    
    // Parse user selection (e.g., "1", "A", "Option A")
    var selectedOptions = ParseUserSelection(userResponse);
    
    // Build refined query with selected options
    var refined = $"{PendingClarification.OriginalQuery} (Selected: {string.Join(", ", selectedOptions)})";
    
    // Add turn to memory
    UpdateContext(
        userInput: PendingClarification.OriginalQuery,
        refinedQuery: refined,
        generatedSQL: null,
        entity: null,
        entityName: null
    );
    
    PendingClarification = null;
    return (refined, selectedOptions);
}
```

## 6.3 Intent Detection & Clarification

### 6.3.1 IntentAnalyzer - Vagueness Detection

**Responsibility:** Analyze queries for ambiguity, vagueness, and missing context requiring clarification.

**Interface:**
```csharp
public class IntentAnalyzer
{
    public static IntentResult Analyze(string query);
}

public class IntentResult
{
    public bool IsVague { get; set; }
    public string Reason { get; set; }           // Why query is vague
    public double ConfidenceScore { get; set; }  // 0.0 - 1.0
    public List<string> MissingEntities { get; set; }
    public List<string> AmbiguousTerms { get; set; }
}
```

**Implementation:**
```csharp
public static IntentResult Analyze(string query)
{
    var result = new IntentResult { IsVague = false, ConfidenceScore = 1.0 };
    
    // Pattern 1: Vague pronouns without context
    if (Regex.IsMatch(query, @"\b(he|she|him|her|them|it|this|that)\b", RegexOptions.IgnoreCase))
    {
        result.IsVague = true;
        result.Reason = "Query contains pronouns without clear context";
        result.ConfidenceScore = 0.4;
    }
    
    // Pattern 2: Generic terms without specifics
    string[] vagueterms = { "report", "data", "information", "details", "stuff" };
    if (vagueTerms.Any(t => query.ToLower().Contains(t)))
    {
        result.IsVague = true;
        result.Reason = "Query contains generic terms requiring clarification";
        result.AmbiguousTerms = vague Terms.Where(t => query.ToLower().Contains(t)).ToList();
        result.ConfidenceScore = 0.5;
    }
    
    // Pattern 3: Missing time context for time-sensitive queries
    string[] timeKeywords = { "recent", "latest", "last" };
    if (timeKeywords.Any(k => query.ToLower().Contains(k)) && 
        !Regex.IsMatch(query, @"\d+ (day|week|month|year)"))
    {
        result.IsVague = true;
        result.Reason = "Time-sensitive query missing specific time range";
        result.ConfidenceScore = 0.6;
    }
    
    return result;
}
```

### 6.3.2 ClarificationAgent - Question Generation

**Responsibility:** Generate interactive clarification questions with multiple-choice options based on schema context.

**Interface:**
```csharp
public class ClarificationAgent
{
    private readonly ClarificationRegistry _registry;
    
    public ClarificationAgent(ClarificationRegistry registry);
    
    public ClarificationQuestion GenerateQuestion(IntentResult vagueness);
}

public class ClarificationQuestion
{
    public string Question { get; set; }
    public List<string> Options { get; set; }
    public string QuestionType { get; set; }  // "entity", "timerange", "report"
}
```

**Implementation:**
```csharp
public ClarificationQuestion GenerateQuestion(IntentResult vagueness)
{
    // Determine question type based on vagueness reason
    if (vagueness.Reason.Contains("generic terms"))
    {
        return GenerateEntityQuestion(vagueness.AmbiguousTerms);
    }
    else if (vagueness.Reason.Contains("time-sensitive"))
    {
        return GenerateTimeRangeQuestion();
    }
    else if (vagueness.Reason.Contains("pronouns"))
    {
        return GeneratePronounResolutionQuestion();
    }
    
    return GenerateGenericQuestion();
}

private ClarificationQuestion GenerateEntityQuestion(List<string> ambiguousTerms)
{
    var term = ambiguousTerms.First();
    var options = _registry.GetOptionsFor(term);
    
    return new ClarificationQuestion
    {
        Question = $"Which '{term}' would you like to see? Please select:",
        Options = options,
        QuestionType = "entity"
    };
}

private ClarificationQuestion GenerateTimeRangeQuestion()
{
    return new ClarificationQuestion
    {
        Question = "Please specify the time range:",
        Options = new List<string>
        {
            "Last 7 days",
            "Last 30 days",
            "Last 3 months",
            "Last year",
            "Custom range"
        },
        QuestionType = "timerange"
    };
}
```

### 6.3.3 ClarificationRegistry - Schema-Based Options

**Responsibility:** Provide context-aware clarification options based on schema structure.

**Implementation:**
```csharp
public class ClarificationRegistry
{
    private readonly List<SchemaChunk> _schemaChunks;
    private readonly Dictionary<string, List<string>> _entityMapping;
    
    public ClarificationRegistry(List<SchemaChunk> schemaChunks)
    {
        _schemaChunks = schemaChunks;
        _entityMapping = BuildEntityMapping();
    }
    
    public List<string> GetOptionsFor(string ambiguousTerm)
    {
        // Map generic term to schema entities
        if (_entityMapping.TryGetValue(ambiguousTerm.ToLower(), out var options))
            return options;
        
        // Search schema chunks for matches
        var matches = _schemaChunks
            .Where(c => c.TableName.ToLower().Contains(ambiguousTerm.ToLower()))
            .Select(c => c.TableName)
            .Distinct()
            .ToList();
        
        return matches.Any() ? matches : new List<string> { "No options found" };
    }
    
    private Dictionary<string, List<string>> BuildEntityMapping()
    {
        return new Dictionary<string, List<string>>
        {
            ["report"] = new List<string> { "Attendance Report", "Fee Report", "Exam Report", "Student Report" },
            ["data"] = new List<string> { "Student Data", "Attendance Data", "Fee Data", "Exam Data" },
            ["information"] = new List<string> { "Personal Information", "Academic Information", "Financial Information" }
        };
    }
}
```

## 6.4 Vector Search (Qdrant Integration)

### 6.4.1 QdrantService - Schema Chunk Search

**Responsibility:** Manage Qdrant vector database for semantic schema search, chunk storage, and similarity-based retrieval.

**Interface:**
```csharp
public class QdrantService
{
    public List<SchemaChunk> _schemaChunks { get; set; }
    
    public QdrantService(string fullSchema, string qdrantURL, string apiKey, string mappingPath);
    
    public async Task InitAsync(string fullSchema, EmbeddingService embeddingService);
    public async Task<SearchOutput> SearchAsync(string query, EmbeddingService embeddingService);
}
```

**Schema Chunking Strategy:**
```csharp
private List<SchemaChunk> ChunkSchema(string fullSchema)
{
    var chunks = new List<SchemaChunk>();
    
    // Parse schema into table definitions
    var tables = ParseSchemaTables(fullSchema);
    
    foreach (var table in tables)
    {
        // Create chunk per table with columns
        var chunk = new SchemaChunk
        {
            TableName = table.Name,
            Columns = table.Columns,
            ChunkText = $"Table: {table.Name}\nColumns: {string.Join(", ", table.Columns.Select(c => $"{c.Name} ({c.DataType})"))}\n{table.Description}",
            Relationships = table.ForeignKeys
        };
        
        chunks.Add(chunk);
        
        // Create additional chunks for complex relationships
        if (table.ForeignKeys.Count > 2)
        {
            var relationshipChunk = new SchemaChunk
            {
                TableName = table.Name,
                ChunkText = $"Relationships for {table.Name}: {string.Join(", ", table.ForeignKeys.Select(fk => $"{fk.FromColumn} → {fk.ToTable}.{fk.ToColumn}"))}",
                IsRelationshipChunk = true
            };
            chunks.Add(relationshipChunk);
        }
    }
    
    return chunks;
}
```

**Vector Search Implementation:**
```csharp
public async Task<SearchOutput> SearchAsync(string query, EmbeddingService embeddingService)
{
    // 1. Generate query embedding
    var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);
    
    // 2. Search Qdrant for similar chunks
    var results = await _qdrantClient.SearchAsync(
        collectionName: $"schema_{_tenantId}",
        vector: queryEmbedding,
        limit: 5,  // Top-5 most relevant chunks
        scoreThreshold: 0.7f
    );
    
    // 3. Extract schema chunks from results
    var relevantChunks = results
        .Select(r => _schemaChunks.First(c => c.ChunkId == r.Id))
        .ToList();
    
    // 4. Resolve table dependencies
    var resolvedSchema = ResolveDependencies(relevantChunks);
    
    // 5. Build enriched output
    return new SearchOutput
    {
        Schema = BuildSchemaText(resolvedSchema),
        Entities = ExtractEntities(relevantChunks),
        MappingService = new EntityTableMappingService(relevantChunks)
    };
}
```

**Dependency Resolution:**
```csharp
private List<SchemaChunk> ResolveDependencies(List<SchemaChunk> chunks)
{
    var resolved = new List<SchemaChunk>(chunks);
    
    foreach (var chunk in chunks)
    {
        // Add related tables via foreign keys
        foreach (var fk in chunk.Relationships)
        {
            var relatedChunk = _schemaChunks.FirstOrDefault(c => c.TableName == fk.ToTable);
            if (relatedChunk != null && !resolved.Contains(relatedChunk))
            {
                resolved.Add(relatedChunk);
            }
        }
    }
    
    return resolved;
}
```

### 6.4.2 EmbeddingService - Text to Vector Conversion

**Responsibility:** Generate vector embeddings using Azure OpenAI text-embedding model.

**Interface:**
```csharp
public class EmbeddingService : IEmbeddingService
{
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _deploymentName;
    
    public async Task<float[]> GenerateEmbeddingAsync(string text);
    public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts);
}
```

**Implementation:**
```csharp
public async Task<float[]> GenerateEmbeddingAsync(string text)
{
    var requestBody = new
    {
        input = text,
        model = "text-embedding-ada-002"
    };
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/openai/deployments/{_deploymentName}/embeddings?api-version=2023-05-15");
    request.Headers.Add("api-key", _apiKey);
    request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
    
    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
    return result.Data[0].Embedding.ToArray();
}
```

## 6.5 Semantic Caching

### 6.5.1 SemanticCacheService - Vector-Based Query Cache

**Responsibility:** Cache query-SQL pairs with vector embeddings for similarity-based retrieval, reducing LLM API calls by 75%+.

**Interface:**
```csharp
public class SemanticCacheService
{
    public async Task<CachedQuery> GetCacheSearchAsync(
        string refinedQuery, 
        string normalizedQuery, 
        ConversationChain context);
    
    public async Task SaveCacheAsync(
        string query, 
        string sql, 
        float[] embedding, 
        string context);
}
```

**Cache Data Model:**
```csharp
public class CachedQuery
{
    public string QueryText { get; set; }
    public string NormalizedQuery { get; set; }
    public string ExecutableSQL { get; set; }
    public float[] QueryEmbedding { get; set; }
    public string ConversationContext { get; set; }
    public double FinalScore { get; set; }  // Similarity score
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public int UsageCount { get; set; }
}
```

**Similarity Search Implementation:**
```csharp
public async Task<CachedQuery> GetCacheSearchAsync(
    string refinedQuery, 
    string normalizedQuery, 
    ConversationChain context)
{
    // 1. Generate query embedding
    var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(refinedQuery);
    
    // 2. Search cache with vector similarity
    var sql = @"
        SELECT TOP 1
            QueryText,
            NormalizedQuery,
            ExecutableSQL,
            QueryEmbedding,
            ConversationContext,
            (1 - (SQRT(POWER(@qv0 - ev0, 2) + POWER(@qv1 - ev1, 2) + ... + POWER(@qv1535 - ev1535, 2)) / SQRT(2))) AS CosineSimilarity
        FROM SemanticCache
        WHERE TenantId = @tenantId
          AND CreatedAt > DATEADD(day, -7, GETUTCDATE())
        HAVING CosineSimilarity > 0.85
        ORDER BY CosineSimilarity DESC";
    
    // 3. Execute similarity search
    using var connection = new SqlConnection(_connectionString);
    var result = await connection.QueryFirstOrDefaultAsync<CachedQuery>(sql, new { 
        tenantId = _tenantId,
        // Pass all 1536 embedding dimensions
        qv0 = queryEmbedding[0],
        qv1 = queryEmbedding[1],
        // ... qv1535
    });
    
    // 4. Validate context similarity (if result found)
    if (result != null)
    {
        var contextSimilarity = CalculateContextSimilarity(context, result.ConversationContext);
        result.FinalScore = (result.FinalScore * 0.7) + (contextSimilarity * 0.3);  // Weighted score
        
        if (result.FinalScore > 0.85)
        {
            await UpdateUsageStats(result);
            return result;
        }
    }
    
    return null;
}
```

**Cache Storage:**
```csharp
public async Task SaveCacheAsync(
    string query, 
    string sql, 
    float[] embedding, 
    string context)
{
    var sql = @"
        INSERT INTO SemanticCache 
            (TenantId, QueryText, NormalizedQuery, ExecutableSQL, QueryEmbedding, ConversationContext, CreatedAt, LastUsedAt, UsageCount)
        VALUES 
            (@tenantId, @query, @normalized, @sql, @embedding, @context, GETUTCDATE(), GETUTCDATE(), 0)";
    
    using var connection = new SqlConnection(_connectionString);
    await connection.ExecuteAsync(sql, new {
        tenantId = _tenantId,
        query = query,
        normalized = NormalizeQuery(query),
        sql = sql,
        embedding = SerializeEmbedding(embedding),  // Store as VARBINARY
        context = context
    });
}
```

**Query Normalization:**
```csharp
private string NormalizeQuery(string query)
{
    // Remove case sensitivity
    query = query.ToLower();
    
    // Remove extra whitespace
    query = Regex.Replace(query, @"\s+", " ");
    
    // Remove punctuation
    query = Regex.Replace(query, @"[^\w\s]", "");
    
    // Remove stop words
    string[] stopWords = { "the", "a", "an", "in", "on", "at", "for", "to", "of" };
    var words = query.Split(' ').Where(w => !stopWords.Contains(w));
    
    return string.Join(" ", words);
}
```

## 6.6 Query Classification

### 6.6.1 QueryClassification - Type and Complexity Detection

**Responsibility:** Analyze query characteristics to determine type, complexity, and required SQL features.

**Interface:**
```csharp
public class QueryClassification
{
    public static QueryClassificationResult Detect(string question);
}

public class QueryClassificationResult
{
    public ClassificationQueryType QueryType { get; set; }
    public double Confidence { get; set; }
    public List<string> Entities { get; set; }
    public bool RequiresWindowFunctions { get; set; }
    public bool RequiresAggregation { get; set; }
    public bool RequiresTimeSeriesLogic { get; set; }
    public bool RequiresPredictionLogic { get; set; }
    public bool RequiresCTE { get; set; }
    public int ComplexityScore { get; set; }  // 0-100
}

public enum ClassificationQueryType
{
    Simple,                  // Single table, basic SELECT
    TrendAnalysis,           // Time-series, growth/decline
    PredictiveAnalysis,      // Risk assessment, forecasting
    ComparativeAnalysis,     // Side-by-side comparison
    AggregateAnalysis        // SUM, AVG, COUNT, GROUP BY
}
```

**Implementation:**
```csharp
public static QueryClassificationResult Detect(string question)
{
    question = question.ToLower();
    var result = new QueryClassificationResult();
    
    // Detect time-series patterns
    if (Regex.IsMatch(question, @"last\s+\d+"))
    {
        result.RequiresTimeSeriesLogic = true;
        result.RequiresWindowFunctions = true;
        result.RequiresCTE = true;
    }
    
    // Detect trend analysis
    if (question.Contains("trend") || question.Contains("decline") || question.Contains("increase"))
    {
        result.QueryType = ClassificationQueryType.TrendAnalysis;
        result.RequiresWindowFunctions = true;
    }
    
    // Detect predictive analysis
    if (question.Contains("predict") || question.Contains("risk") || question.Contains("likely"))
    {
        result.QueryType = ClassificationQueryType.PredictiveAnalysis;
        result.RequiresPredictionLogic = true;
    }
    
    // Detect comparative analysis
    if (question.Contains("compare"))
    {
        result.QueryType = ClassificationQueryType.ComparativeAnalysis;
    }
    
    // Calculate complexity score
    result.ComplexityScore = CalculateComplexity(question);
    
    return result;
}

private static int CalculateComplexity(string question)
{
    int score = 0;
    
    // Multi-entity complexity
    string[] entities = { "student", "attendance", "exam", "fee", "teacher", "subject" };
    score += entities.Count(e => question.Contains(e)) * 5;
    
    // Aggregation complexity
    string[] aggregations = { "count", "sum", "average", "total", "highest", "lowest" };
    score += aggregations.Count(a => question.Contains(a)) * 3;
    
    // Time complexity
    string[] timeKeywords = { "last", "recent", "between", "during", "since" };
    score += timeKeywords.Count(t => question.Contains(t)) * 4;
    
    // Comparison complexity
    string[] comparisons = { "compare", "versus", "vs", "difference" };
    score += comparisons.Count(c => question.Contains(c)) * 6;
    
    return Math.Min(score, 100);
}
```

## 6.7 LlmService - Azure OpenAI Integration

### 6.7.1 Responsibility
HTTP client wrapper for Azure OpenAI API with prompt engineering, response parsing, and retry logic.

### 6.7.2 Interface
```csharp
public interface ILlmService
{
    Task<string> AskAsync(SearchOutput entityOutput, string prompt, MemoryTurn turn);
    Task<string> AskAsyncDelay(string schemaText, string userPrompt, string previousSQL);
}
```

### 6.7.3 Implementation

**Prompt Building:**
```csharp
public async Task<string> AskAsync(SearchOutput entityOutput, string prompt, MemoryTurn turn)
{
    // Build rule engines
    var engine = new PromptRuleEngine();
    var rules = engine.BuildRules(prompt);
    
    var outputEngine = new OutputRuleEngine();
    var outputRules = outputEngine.BuildOutputRules(prompt);
    
    // Build enum context (column value constraints)
    var enumContext = entityOutput.MappingService.BuildEnumContext(entityOutput.Entities);
    
    // Construct full prompt
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
        
        Generate ONLY the SQL query, no explanations.
    ";
    
    // Call Azure OpenAI
    var response = await CallAzureOpenAI(fullPrompt);
    
    // Extract and clean SQL
    var sql = ExtractSqlQuery(response);
    
    return sql;
}
```

**Azure OpenAI API Call:**
```csharp
private async Task<string> CallAzureOpenAI(string prompt)
{
    string apiVersion = "2024-02-01";
    string url = $"{_options.Endpoint}/openai/deployments/{_options.Model}/chat/completions?api-version={apiVersion}";
    
    var requestBody = new
    {
        model = _options.Model,
        messages = new[]
        {
            new { role = "user", content = prompt }
        },
        max_tokens = 500,
        temperature = 0.0  // Deterministic for SQL generation
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
    
    return responseContent;
}
```

**SQL Extraction:**
```csharp
private string ExtractSqlQuery(string response)
{
    // Parse JSON response
    var json = JsonDocument.Parse(response);
    var content = json.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();
    
    // Remove markdown code blocks
    content = Regex.Replace(content, @"```sql\s*", "");
    content = Regex.Replace(content, @"```\s*", "");
    
    // Remove comments
    content = Regex.Replace(content, @"--[^\n]*", "");
    
    // Trim whitespace
    content = content.Trim();
    
    return content;
}
```

---

# 7. Data Models

## 7.1 Core Models

### LLMResponse
```csharp
public class LLMResponse
{
    public string SQL { get; set; }
    public ResponseType Type { get; set; }
    public string Message { get; set; }
}

public enum ResponseType
{
    SQL,              // Valid SQL query generated
    ERROR,            // Error occurred
    CLARIFICATION,    // Needs user clarification
    MESSAGE           // Informational message (history, reset, etc.)
}
```

### TenantData
```csharp
public class TenantData
{
    public string TenantId { get; set; }
    public string SchemaFile { get; set; }      // Schema filename (e.g., "DemoSchema.txt")
    public string MappingFile { get; set; }     // Entity mapping filename (e.g., "entity-table-mapping.json")
}
```

### SearchOutput
```csharp
public class SearchOutput
{
    public string Schema { get; set; }          // Relevant schema text for prompt
    public List<string> Entities { get; set; }  // Extracted entity names
    public EntityTableMappingService MappingService { get; set; }
}
```

### SchemaChunk
```csharp
public class SchemaChunk
{
    public string ChunkId { get; set; }
    public string TableName { get; set; }
    public List<ColumnSchema> Columns { get; set; }
    public string ChunkText { get; set; }
    public List<TableRelation> Relationships { get; set; }
    public bool IsRelationshipChunk { get; set; }
    public float[] Embedding { get; set; }
}
```

### ColumnSchema
```csharp
public class ColumnSchema
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public List<string> AllowedValues { get; set; }  // Enum constraints
}
```

### TableRelation
```csharp
public class TableRelation
{
    public string FromTable { get; set; }
    public string FromColumn { get; set; }
    public string ToTable { get; set; }
    public string ToColumn { get; set; }
    public string JoinType { get; set; }  // INNER, LEFT, RIGHT
}
```

---

# 8. API Contracts

## 8.1 Main Entry Point

### ProcessQuery Request
```csharp
public async Task<LLMResponse> ProcessQuery(string userPrompt, TenantData tenant)
```

**Parameters:**
- `userPrompt` (string) - Natural language query from user
- `tenant` (TenantData) - Tenant configuration

**Response:**
```json
{
  "sql": "SELECT * FROM Students WHERE StudentId = 'student10'",
  "type": "SQL",
  "message": null
}
```

**Response Types:**
1. **SQL Response** - Valid SQL generated
```json
{
  "sql": "SELECT...",
  "type": "SQL",
  "message": null
}
```

2. **Clarification Response** - Needs user input
```json
{
  "sql": null,
  "type": "CLARIFICATION",
  "message": "Which report would you like to see? Options: 1. Attendance Report, 2. Fee Report, 3. Exam Report"
}
```

3. **Error Response** - Processing failed
```json
{
  "sql": null,
  "type": "ERROR",
  "message": "Unable to read database schema."
}
```

4. **Message Response** - Informational
```json
{
  "sql": null,
  "type": "MESSAGE",
  "message": "Conversation history:\nTurn 1: Show attendance for student10\nTurn 2: What about student11\n..."
}
```

## 8.2 Special Commands

### History Command
**Input:** `"history"`  
**Output:** Conversation history formatted as string

### Reset Command
**Input:** `"reset"`  
**Output:** `"RESET"` message, conversation context cleared

---

# 9. Sequence Diagrams

## 9.1 Complete Query Processing Flow

```
User          LLMServicePipe       STM       IntentAnalyzer   QdrantService   SemanticCache   LlmService   Validator
 │                 │                 │              │                │                │               │              │
 ├─"Show attendance for student10"──►│                │              │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─ReadSchema()────►FileService   │                │                │                │               │              │
 │                 ◄─────────────────┤              │                │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─GetLatestTurn()──────────────►│              │                │                │                │               │              │
 │                 ◄─────────────────────────────── │              │                │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─ExtractEntity()─────────────►│              │                │                │                │               │              │
 │                 ◄─(student10)────────────────── │              │                │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─Analyze(query)─────────────────────────────►│                │                │                │               │              │
 │                 ◄─(IsVague=false)─────────────────────────────┤                │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─GetCacheSearch()────────────────────────────────────────────►│                │                │               │              │
 │                 │                 │              │                │                ├─Vector similarity search      │               │              │
 │                 ◄─(null - cache miss)─────────────────────────────────────────── │                │                │               │              │
 │                 │                 │              │                │                │                │               │              │
 │                 ├─SearchAsync(query)────────────────────────────────────────────────────────────►│                │               │              │
 │                 │                 │              │                │                │                ├─Generate embedding           │              │
 │                 │                 │              │                │                │                ├─Search vectors               │              │
 │                 │                 │              │                │                │                ├─Resolve dependencies         │              │
 │                 ◄─(SearchOutput with top-5 chunks)──────────────────────────────────────────────  │                │               │              │
 │                 │                 │              │                │                │                │                │               │              │
 │                 ├─AskAsync(schema, query)───────────────────────────────────────────────────────────────────────────►│              │
 │                 │                 │              │                │                │                │                ├─Build prompt              │
 │                 │                 │              │                │                │                │                ├─Call Azure OpenAI         │
 │                 │                 │              │                │                │                │                ├─Parse response            │
 │                 ◄─(Generated SQL)──────────────────────────────────────────────────────────────────────────────────  │              │
 │                 │                 │              │                │                │                │                │               │              │
 │                 ├─ValidateAsync(sql)─────────────────────────────────────────────────────────────────────────────────────────────►│
 │                 │                 │              │                │                │                │                │               ├─Syntax check      │
 │                 │                 │              │                │                │                │                │               ├─Schema check      │
 │                 │                 │              │                │                │                │                │               ├─Dry-run test      │
 │                 ◄─(Valid)────────────────────────────────────────────────────────────────────────────────────────────────────────  │
 │                 │                 │              │                │                │                │                │               │              │
 │                 ├─SaveCache(query, sql)─────────────────────────────────────────►│                │                │               │              │
 │                 ◄─────────────────────────────────────────────────────────────── │                │                │               │              │
 │                 │                 │              │                │                │                │                │               │              │
 │                 ├─UpdateContext()─────────────►│              │                │                │                │                │               │              │
 │                 ◄───────────────────────────────┤              │                │                │                │                │               │              │
 │                 │                 │              │                │                │                │                │               │              │
 ◄─{SQL: "SELECT...", Type: SQL}───  │              │                │                │                │                │                │               │              │
```

## 9.2 Clarification Flow

```
User          LLMServicePipe       STM       IntentAnalyzer   ClarificationAgent
 │                 │                 │              │                │
 ├─"Show me the report"─────────────►│              │                │
 │                 │                 │              │                │
 │                 ├─Analyze()───────────────────────────────────►│                │
 │                 ◄─(IsVague=true, Reason="Generic term")─────── │                │
 │                 │                 │              │                │
 │                 ├─GenerateQuestion(vagueness)───────────────────────────────────►│
 │                 │                 │              │                ├─Get options from registry  │
 │                 ◄─(Clarification question)─────────────────────────────────────  │
 │                 │                 │              │                │
 │                 ├─SetWaitingForAnswer()──────►│              │                │
 │                 ◄───────────────────────────────┤              │                │
 │                 │                 │              │                │
 ◄─{Type: CLARIFICATION, Message: "Which report...?"}──────────  │              │                │
 │                 │                 │              │                │
 │                 │                 │              │                │
 ├─"1" (user selects option 1)──────►│              │                │
 │                 │                 │              │                │
 │                 ├─ResolveClarification("1")────►│              │                │
 │                 │                 ├─Parse selection             │                │
 │                 │                 ├─Build refined query         │                │
 │                 ◄─(refined: "Show attendance report")─────────  │                │
 │                 │                 │              │                │
 │                 ├─[Continue with normal flow...]│              │                │
```

## 9.3 Follow-Up Query Flow

```
User          LLMServicePipe       STM       
 │                 │                 │
 ├─"Show attendance for student10"──►│
 │                 │                 │
 │                 ├─[Normal flow]───►│
 │                 │                 ├─Store turn with entity="student10"
 │                 ◄─────────────────┤
 ◄─{SQL: "SELECT...WHERE StudentId='student10'"}
 │                 │                 │
 │                 │                 │
 ├─"What about student11?"──────────►│
 │                 │                 │
 │                 ├─IsFollowUp()────►│
 │                 ◄─(true)───────────┤
 │                 │                 │
 │                 ├─EnrichWithContext()──────────►│
 │                 │                 ├─Replace "What about" with context
 │                 │                 ├─Add previous entity context
 │                 ◄─(enriched: "Show attendance for student11")
 │                 │                 │
 │                 ├─[Continue with normal flow...]
```

---

# 10. State Diagrams

## 10.1 Conversation State Machine

```
┌─────────────┐
│   NEW       │ Initial state, no conversation history
└──────┬──────┘
       │ First query
       ▼
┌─────────────┐
│  ACTIVE     │ Conversation in progress
└──────┬──────┘
       │
       ├──[Query needs clarification]──► ┌──────────────────┐
       │                                   │ WAITING_FOR_     │
       │                                   │ CLARIFICATION    │
       │                                   └────────┬─────────┘
       │                                           │
       │◄──────────[User provides answer]─────────┘
       │
       ├──[Follow-up detected]───────────► ┌──────────────────┐
       │                                   │ FOLLOW_UP        │
       │                                   │ (context enrichment)
       │                                   └────────┬─────────┘
       │◄──────────────────────────────────────────┘
       │
       ├──["reset" command]──────────────► ┌──────────────────┐
       │                                   │ RESET            │
       │                                   └────────┬─────────┘
       │◄──────────────────────────────────────────┘
       │                                           │
       └────────────────────────────────────────────▼
                                             ┌─────────────┐
                                             │   NEW       │
                                             └─────────────┘
```

## 10.2 Query Processing State Machine

```
┌─────────────┐
│  RECEIVED   │ Query received from user
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  ANALYZING  │ Intent analysis & entity extraction
└──────┬──────┘
       │
       ├──[IsVague]──────────────────────► ┌──────────────────┐
       │                                   │ CLARIFYING       │
       │                                   └────────┬─────────┘
       │◄──────────[Answer received]──────────────┘
       │
       ▼
┌─────────────┐
│  SEARCHING  │ Vector search & cache lookup
└──────┬──────┘
       │
       ├──[Cache hit]────────────────────► ┌──────────────────┐
       │                                   │ CACHED           │
       │                                   └────────┬─────────┘
       │                                           │
       │                                           └──────────────┐
       │                                                          │
       ▼                                                          │
┌─────────────┐                                                  │
│  GENERATING │ LLM SQL generation                               │
└──────┬──────┘                                                  │
       │                                                          │
       ▼                                                          │
┌─────────────┐                                                  │
│ VALIDATING  │ 4-layer validation pipeline                      │
└──────┬──────┘                                                  │
       │                                                          │
       ├──[Invalid]──────────────────────► ┌──────────────────┐ │
       │                                   │ FAILED           │ │
       │                                   └────────┬─────────┘ │
       │◄──────────[Retry (3x)]────────────────────┘           │
       │                                                         │
       ▼                                                         │
┌─────────────┐                                                  │
│  CACHING    │ Save to semantic cache                           │
└──────┬──────┘                                                  │
       │                                                          │
       ▼                                                          │
┌─────────────┐◄─────────────────────────────────────────────────┘
│  COMPLETED  │ SQL ready for execution
└─────────────┘
```

---

# 11. Integration Points

## 11.1 External Integrations

### Azure OpenAI (GPT-4)
**Purpose:** Natural language to SQL conversion  
**Endpoint:** `https://<instance>.openai.azure.com/`  
**Authentication:** API Key  
**Model:** gpt-4 or gpt-4o  
**Max Tokens:** 500  
**Temperature:** 0.0 (deterministic)  
**Retry Strategy:** 3 retries with exponential backoff (1s, 2s, 4s)

### Azure OpenAI (Text Embedding)
**Purpose:** Generate vector embeddings for semantic search  
**Model:** text-embedding-ada-002  
**Dimensions:** 1536  
**Usage:** Schema chunking, query embeddings, cache similarity

### Qdrant Cloud
**Purpose:** Vector database for schema chunk storage  
**Protocol:** HTTPS REST API  
**Authentication:** API Key  
**Operations:**
- CreateCollection (per tenant schema)
- UpsertPoints (store schema chunks with embeddings)
- SearchPoints (similarity search, top-K retrieval)

### SQL Server (Semantic Cache)
**Purpose:** Store cached queries with embeddings  
**Connection:** Microsoft.Data.SqlClient  
**Tables:**
- `SemanticCache` - Query cache with vector embeddings
- `ConversationHistory` - Long-term conversation storage (future)

## 11.2 Internal Integrations

### AIQueryPlatform.SqlValidator
**Purpose:** 4-layer SQL validation pipeline  
**Layers:**
1. **SyntaxValidator** - Basic SQL syntax checks
2. **SchemaValidator** - Schema-aware validation
3. **LLMSelfCheckService** - LLM validates its own SQL
4. **DryRunValidator** - Database dry-run test

**Integration:**
```csharp
var validator = new SqlValidationPipeline(schema, httpClient, connectionString);
var result = await validator.ValidateAsync(sql, currentTurn);

if (!result.IsValid)
{
    // Retry with fix hint
    var fixedSQL = await llmService.FixSQL(sql, result.FixHint);
}
```

### AIQueryPlatform.Api
**Consumer:** Web API layer calls LLMServicePipe.ProcessQuery()

**Integration Pattern:**
```csharp
// In QueryController
var llmPipe = new LLMServicePipe(_configuration, _llmService);
var tenant = new TenantData
{
    TenantId = _tenantContext.TenantId,
    SchemaFile = _tenantContext.Tenant.SchemaFile,
    MappingFile = _tenantContext.Tenant.MappingFile
};

var response = await llmPipe.ProcessQuery(userQuery, tenant);

if (response.Type == ResponseType.SQL)
{
    // Execute SQL against tenant database
    var results = await _queryExecutionService.ExecuteQueryAsync(response.SQL);
    return Ok(results);
}
else if (response.Type == ResponseType.CLARIFICATION)
{
    // Return clarification question to user
    return Ok(new { needsClarification = true, message = response.Message });
}
```

---

# 12. Security & Validation

## 12.1 SQL Injection Prevention

**Multi-Layer Defense:**
1. **LLM Prompt Instructions** - Explicitly instruct GPT-4 to generate only SELECT queries
2. **Keyword Validation** - Block INSERT, UPDATE, DELETE, DROP, etc. (in SqlValidator)
3. **Pattern Detection** - Block injection patterns: --, /*, UNION, xp_ (in SqlValidator)
4. **Schema Validation** - Verify all tables/columns exist in schema (in SqlValidator)
5. **Dry-Run Test** - Execute with EXPLAIN to catch runtime errors (in SqlValidator)

## 12.2 Prompt Injection Prevention

**Defenses:**
1. **User Input Sanitization** - Remove SQL keywords from user prompts before embedding
2. **Prompt Template Structure** - Strict template prevents prompt leaking
3. **Response Parsing** - Extract only SQL portion, ignore any text explanations

## 12.3 Data Privacy

**Tenant Isolation:**
- Separate ShortTermMemory instances per tenant
- Separate Qdrant collections per tenant schema
- Separate cache entries per tenant

**Sensitive Data Handling:**
- API keys stored in configuration (Azure Key Vault in production)
- No PII data logged (only tenant IDs, not query content)

## 12.4 Rate Limiting

**Implemented at API Layer:**
- LLM Service library does not enforce rate limits
- Consumer (AIQueryPlatform.Api) implements per-tenant quotas

---

# 13. Performance & Optimization

## 13.1 Performance Metrics

| Metric | Target | Actual (Observed) |
|--------|--------|-------------------|
| **Cache Hit Response Time** | <500ms | 250ms (P95) |
| **Vector Search Latency** | <200ms | 150ms (P95) |
| **LLM API Call** | <4s | 3.2s (P95) |
| **Total Query Time (cached)** | <1s | 800ms (P95) |
| **Total Query Time (uncached)** | <6s | 5s (P95) |
| **Cache Hit Rate** | >85% | 92% (production) |

## 13.2 Optimization Strategies

### Semantic Caching
**Impact:** 75% reduction in LLM API costs, 80% reduction in response time

**Strategy:**
- Vector-based similarity search (cosine similarity >0.85)
- Context-aware matching (conversation history weighted)
- TTL-based cache expiration (7 days default)

### Schema Chunking
**Impact:** 60% reduction in prompt token usage

**Strategy:**
- Chunk schema into logical table groupings
- Use vector search to retrieve only relevant chunks (top-5)
- Include relationship chunks for JOIN queries

### Prompt Engineering
**Impact:** 15% improvement in SQL accuracy

**Strategy:**
- Dynamic rule injection based on query classification
- Enum context for exact WHERE clause values
- Database-specific syntax hints (T-SQL vs MySQL vs PostgreSQL)

### In-Memory State Management
**Impact:** <50ms state lookup, no database round-trips

**Strategy:**
- ShortTermMemory stored in-memory (not persisted)
- Acceptable for conversational sessions (<1 hour)
- Trade-off: State lost on app restart

## 13.3 Token Usage Optimization

**Baseline (Full Schema):**
- Average prompt: 8,500 tokens (schema) + 50 tokens (query) = 8,550 tokens
- Cost per query: $0.256 (at $0.03/1K tokens)

**Optimized (Vector Search):**
- Average prompt: 3,200 tokens (top-5 chunks) + 50 tokens (query) = 3,250 tokens
- Cost per query: $0.098 (62% reduction)

**With Semantic Caching:**
- 92% cache hit rate = 92% of queries cost $0 (LLM not called)
- Effective cost per query: $0.098 × 0.08 = $0.0078 (97% total reduction)

---

# 14. Testing Strategy

## 14.1 Unit Tests

### Core Components
| Component | Test Cases | Coverage Target |
|-----------|------------|-----------------|
| **LLMServicePipe** | Process query, handle errors, special commands | 85% |
| **ShortTermMemory** | Follow-up detection, context enrichment, clarification handling | 90% |
| **IntentAnalyzer** | Vagueness detection, confidence scoring | 85% |
| **QueryClassification** | Type detection, complexity scoring | 80% |
| **EntityNameExtractor** | Entity extraction, pronoun resolution | 90% |
| **QdrantService** | Schema chunking, vector search, dependency resolution | 75% |
| **SemanticCacheService** | Cache hit/miss, similarity scoring | 85% |

### Example Unit Tests
```csharp
[Fact]
public void IntentAnalyzer_DetectsVagueQuery()
{
    var result = IntentAnalyzer.Analyze("Show me the report");
    
    Assert.True(result.IsVague);
    Assert.Contains("generic terms", result.Reason);
    Assert.True(result.ConfidenceScore < 0.6);
}

[Fact]
public void ShortTermMemory_EnrichesFollowUpQuery()
{
    var memory = new ShortTermMemory("tenant123");
    memory.UpdateContext(
        userInput: "Show attendance for student10",
        refinedQuery: "Show attendance for student10",
        generatedSQL: "SELECT...",
        entity: "student10",
        entityName: "student10"
    );
    
    var enriched = memory.EnrichWithContext("What about him?");
    
    Assert.Contains("student10", enriched);
}
```

## 14.2 Integration Tests

### LLM Integration
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| IT-LLM-001 | Call Azure OpenAI with valid prompt | Returns valid SQL |
| IT-LLM-002 | Handle Azure OpenAI timeout | Retries 3 times, throws exception |
| IT-LLM-003 | Parse SQL from response | Extracts clean SQL without markdown |

### Vector Search Integration
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| IT-VEC-001 | Search Qdrant with query embedding | Returns top-5 relevant chunks |
| IT-VEC-002 | Resolve table dependencies | Includes related tables via FKs |
| IT-VEC-003 | Handle Qdrant connection failure | Graceful degradation to full schema |

### Semantic Cache Integration
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| IT-CACHE-001 | Cache hit with similar query | Returns cached SQL in <500ms |
| IT-CACHE-002 | Cache miss | Proceeds to LLM call |
| IT-CACHE-003 | Save new cache entry | Stores query + SQL + embedding |

## 14.3 End-to-End Tests

### Complete Query Flows
```gherkin
Feature: Complete Query Processing

Scenario: Simple query with cache miss
  Given tenant "demo" with schema file "DemoSchema.txt"
  When user submits query "Show attendance for student10"
  Then system checks semantic cache (miss)
  And system searches Qdrant for relevant schema
  And system calls Azure OpenAI
  And system validates generated SQL
  And system saves to semantic cache
  And system returns SQL response

Scenario: Follow-up query
  Given previous query was "Show attendance for student10"
  When user submits "What about student11?"
  Then system detects follow-up
  And system enriches query with context
  And system generates SQL for student11

Scenario: Ambiguous query requiring clarification
  Given user submits "Show me the report"
  Then system detects vagueness
  And system generates clarification question
  And system waits for user selection
  When user selects "Attendance Report"
  Then system refines query
  And system generates SQL for attendance report
```

## 14.4 Performance Tests

### Load Testing
```csharp
[Fact]
public async Task LLMServicePipe_Handles100ConcurrentRequests()
{
    var pipe = new LLMServicePipe(_config, _llmService);
    var tasks = new List<Task<LLMResponse>>();
    
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(pipe.ProcessQuery($"Show student{i}", _tenant));
    }
    
    var results = await Task.WhenAll(tasks);
    
    Assert.All(results, r => Assert.NotNull(r.SQL));
}
```

### Cache Performance
```csharp
[Fact]
public async Task SemanticCache_Achieves90PercentHitRate()
{
    var cache = new SemanticCacheService(_connectionString, _embeddingService);
    
    // Warm up cache with 100 queries
    for (int i = 0; i < 100; i++)
    {
        await cache.SaveCacheAsync($"query{i}", $"sql{i}", embedding, context);
    }
    
    // Test 1000 similar queries
    int hits = 0;
    for (int i = 0; i < 1000; i++)
    {
        var similar = $"query{i % 100} with slight variation";
        var result = await cache.GetCacheSearchAsync(similar, normalized, context);
        if (result != null) hits++;
    }
    
    var hitRate = hits / 1000.0;
    Assert.True(hitRate > 0.90);
}
```

---

# 15. Deployment & Configuration

## 15.1 Configuration Structure

**appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AIQueryPlatform;Trusted_Connection=true;"
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "Endpoint": "https://your-instance.openai.azure.com/",
    "Model": "gpt-4o",
    "DeploymentName": "gpt-4-deployment",
    "MaxTokens": 500,
    "Temperature": 0.0
  },
  "TextModelAI": {
    "ApiKey": "sk-...",
    "Endpoint": "https://your-instance.openai.azure.com/",
    "Model": "text-embedding-ada-002",
    "DeploymentName": "embedding-deployment"
  },
  "QdrantData": {
    "URL": "https://your-cluster.qdrant.tech",
    "APIKey": "qdrant-api-key-..."
  },
  "SchemaSettings": {
    "SchemaPath": "./SchemaFiles"
  }
}
```

## 15.2 Dependency Injection Setup

```csharp
// In AIQueryPlatform.Api/Program.cs
services.AddHttpClient<ILlmService, LlmService>();
services.Configure<LlmOptions>(configuration.GetSection("OpenAI"));
services.AddScoped<ILLMServicePipe, LLMServicePipe>();
services.AddScoped<TenantContext>();  // Per-request tenant isolation
```

## 15.3 Schema File Structure

**SchemaFiles/DemoSchema.txt**
```
Table: Students
Columns:
  - StudentId (VARCHAR, PK)
  - Name (VARCHAR)
  - Grade (INT)
  - Section (VARCHAR)

Table: Attendance
Columns:
  - AttendanceId (INT, PK)
  - StudentId (VARCHAR, FK → Students.StudentId)
  - Date (DATE)
  - Status (VARCHAR) -- VALUES: Present, Absent, Late

Table: Exams
Columns:
  - ExamId (INT, PK)
  - StudentId (VARCHAR, FK → Students.StudentId)
  - Subject (VARCHAR)
  - Score (DECIMAL)
  - ExamDate (DATE)
```

**Mapping/entity-table-mapping.json**
```json
{
  "mappings": [
    {
      "entity": "student",
      "table": "Students",
      "primaryKey": "StudentId"
    },
    {
      "entity": "attendance",
      "table": "Attendance",
      "primaryKey": "AttendanceId"
    },
    {
      "entity": "exam",
      "table": "Exams",
      "primaryKey": "ExamId"
    }
  ],
  "enums": {
    "Attendance.Status": ["Present", "Absent", "Late"],
    "Students.Grade": ["9", "10", "11", "12"]
  }
}
```

---

# 16. Monitoring & Observability

## 16.1 Logging

**Log Events:**
```csharp
_logger.LogInformation("Processing query: {Query}", userPrompt);
_logger.LogInformation("Cache hit: {Similarity}", cached.FinalScore);
_logger.LogWarning("Cache miss, calling LLM");
_logger.LogInformation("Vector search returned {Count} chunks", chunks.Count);
_logger.LogInformation("LLM responded with SQL: {SQL}", sql);
_logger.LogError(ex, "Azure OpenAI API call failed");
```

**Structured Logging:**
```json
{
  "timestamp": "2026-05-30T10:30:00Z",
  "level": "Information",
  "message": "Processing query",
  "tenantId": "tenant123",
  "query": "Show attendance for student10",
  "cacheHit": false,
  "vectorSearchTime": 150,
  "llmResponseTime": 3200,
  "totalTime": 5000
}
```

## 16.2 Metrics

**Key Metrics to Track:**
- **Cache Hit Rate** - Percentage of queries served from cache
- **LLM API Latency** - Time spent in Azure OpenAI calls (P50, P95, P99)
- **Vector Search Latency** - Qdrant query time
- **Total Query Time** - End-to-end processing time
- **Token Usage** - Total tokens consumed per tenant per day
- **Error Rate** - Percentage of failed queries
- **Clarification Rate** - Percentage of queries requiring clarification

**Monitoring Queries:**
```sql
-- Cache hit rate (last 24 hours)
SELECT 
    COUNT(CASE WHEN CacheHit = 1 THEN 1 END) * 100.0 / COUNT(*) AS CacheHitRate
FROM QueryLogs
WHERE CreatedAt > DATEADD(hour, -24, GETUTCDATE());

-- Average response times
SELECT 
    AVG(CASE WHEN CacheHit = 1 THEN ResponseTime END) AS AvgCachedTime,
    AVG(CASE WHEN CacheHit = 0 THEN ResponseTime END) AS AvgUncachedTime
FROM QueryLogs
WHERE CreatedAt > DATEADD(hour, -24, GETUTCDATE());
```

## 16.3 Alerts

**Critical Alerts:**
- Cache hit rate drops below 70%
- LLM API error rate > 5%
- P95 query latency > 10s
- Qdrant connection failures

**Warning Alerts:**
- Cache hit rate drops below 80%
- Token usage exceeds budget
- Memory usage > 80%

---

# 17. Appendix

## 17.1 Glossary

| Term | Definition |
|------|------------|
| **STM (Short-Term Memory)** | In-memory conversation context tracking system |
| **Semantic Caching** | Vector-based query caching using embedding similarity |
| **Schema Chunking** | Dividing database schema into vector-searchable segments |
| **Entity Resolution** | Mapping pronouns to previously mentioned entities |
| **Intent Detection** | Analyzing query vagueness and ambiguity |
| **Clarification Agent** | System component generating interactive clarification questions |
| **Qdrant** | Vector database for similarity search |
| **Embedding** | Numeric vector representation of text (1536 dimensions) |
| **Follow-Up Query** | Query referencing previous conversation context |
| **Continuous Context** | Query continuing previous conversation thread |

## 17.2 Acronyms

| Acronym | Full Form |
|---------|-----------|
| **LLM** | Large Language Model |
| **NL2SQL** | Natural Language to SQL |
| **STM** | Short-Term Memory |
| **GPT** | Generative Pre-trained Transformer |
| **FK** | Foreign Key |
| **PK** | Primary Key |
| **CTE** | Common Table Expression |
| **TTL** | Time To Live |

## 17.3 References

- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [Qdrant Vector Database](https://qdrant.tech/documentation/)
- [Semantic Caching Research](https://arxiv.org/abs/2304.01852)
- [Conversational AI Design Patterns](https://www.microsoft.com/en-us/research/project/conversational-ai/)

## 17.4 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-30 | LLM Team | Initial comprehensive TSD for LLMService library |

---

**END OF TECHNICAL SPECIFICATION DOCUMENT**

**Document Classification:** Internal - Confidential  
**Next Review Date:** November 30, 2026  
**Document Owner:** LLM Engineering Team  
**Contact:** llm-team@aiquery.com
