# Technical Specification Document
## AI-Powered Multi-Tenant Query & Reporting Platform

**Version:** 1.0  
**Date:** May 30, 2026  
**Status:** Production  
**Document Owner:** Development Team  
**Classification:** Internal - Confidential

---

## Project Information

**Project Name:** AIQueryPlatform

**Business Objective:** Democratize data access by enabling non-technical users to query databases using natural language, eliminating the need for SQL knowledge while maintaining enterprise-grade security, multi-tenancy, and governance.

**Users/Roles:**
- **Platform Admin** - Manages tenants, monitors system health, configures platform settings
- **Tenant Admin** - Manages tenant configuration, branding, database connections
- **Business User** - End users executing natural language queries via widget/SDK
- **Developer** - Integrates SDK into applications, builds custom solutions

**Technology Stack:**
- **Backend:** ASP.NET Core (.NET 8), C#
- **Frontend:** React.js 18.x (TypeScript), Webpack 5
- **Database:** SQL Server (primary), MySQL, PostgreSQL, SQLite, Excel (multi-database support)
- **Authentication:** API Key-based per-tenant authentication
- **AI/ML:** Azure OpenAI (GPT-4), OpenAI API (fallback)
- **Hosting:** Azure App Service (Web API), Azure Function App (future serverless option)
- **Caching:** In-Memory Cache (IMemoryCache)
- **Logging:** Serilog with Application Insights integration
- **Containerization:** Docker with docker-compose

---

# 1. Executive Summary

## 1.1 Overview

The AI-Powered Multi-Tenant Query & Reporting Platform is an enterprise-grade solution that bridges the gap between business users and their data. By leveraging Large Language Models (LLMs), specifically Azure OpenAI GPT-4, the platform converts natural language questions into secure SQL queries, executes them against tenant-isolated databases, and presents results with intelligent visualization recommendations.

## 1.2 Business Goals

**Primary Goals:**
1. **Democratize Data Access** - Enable non-technical users to query databases without SQL knowledge
2. **Ensure Data Security** - Provide enterprise-grade security with tenant isolation and SELECT-only enforcement
3. **Accelerate Decision Making** - Deliver real-time query results with streaming responses
4. **Reduce IT Burden** - Minimize ad-hoc query requests to IT/BI teams
5. **Enable Self-Service Analytics** - Empower business users with self-service data exploration

**Success Metrics:**
- 80% reduction in ad-hoc SQL query requests to IT teams
- <5 second average query response time
- 95% query accuracy (correct SQL generation)
- 99.5% system uptime
- Support for 100+ concurrent tenants

## 1.3 Key Features

### Core Features
- **Natural Language to SQL (NL2SQL)** - AI-powered query conversion using GPT-4
- **Multi-Tenant Architecture** - Complete database-level isolation per tenant
- **Real-Time Streaming** - NDJSON streaming responses for progressive UI updates
- **Security Hardening** - SELECT-only enforcement, SQL injection prevention, rate limiting
- **Intelligent Visualization** - Auto-detection of optimal display format (table/chart/PDF)
- **PDF Report Generation** - Branded reports with tenant logos and theme colors
- **Conversation History** - Query tracking with metadata for audit and reuse
- **Token Usage Analytics** - OpenAI cost tracking per tenant
- **Multi-Database Support** - SQL Server, MySQL, PostgreSQL, SQLite, Excel

### Integration Features
- **JavaScript SDK** - Easy integration for custom web applications
- **Embeddable Widget** - Plug-and-play widget with Shadow DOM isolation
- **REST API** - Standard HTTP/JSON API for any client platform
- **Streaming API** - Server-sent events via NDJSON for real-time updates

### Enterprise Features
- **Schema Discovery** - Automatic database schema detection with caching
- **Rate Limiting** - Per-tenant quotas (60/min, 1000/hour)
- **Comprehensive Logging** - Structured logging with Serilog
- **Error Handling** - Global exception middleware with sanitized responses
- **Tenant Branding** - Custom logos and theme colors per tenant

## 1.4 Technical Architecture (High-Level)

The platform follows a **Layered Architecture** with **Clean Architecture** principles:

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Layer                             │
│  React Widget (Shadow DOM) | JavaScript SDK | REST Clients │
└─────────────────────────┬───────────────────────────────────┘
                          │ HTTPS/TLS 1.2+
┌─────────────────────────▼───────────────────────────────────┐
│                  API Gateway Layer                          │
│  Middleware: Exception → Tenant → Rate Limit → CORS        │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                  Controllers Layer                          │
│  Query | Tenant | Conversations | Analyses | TokenUsage    │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                   Services Layer                            │
│  Orchestration | NL2SQL | Validation | Execution |         │
│  Intelligence | Schema | Reporting | Conversations          │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                   Data Layer                                │
│  Platform DB | Tenant DBs | Memory Cache | Azure OpenAI    │
└─────────────────────────────────────────────────────────────┘
```

**Key Architectural Patterns:**
- **Multi-Tenancy:** Database-per-tenant with scoped TenantContext
- **Streaming:** IAsyncEnumerable with yield return for memory efficiency
- **Dependency Injection:** Native ASP.NET Core DI container
- **Repository Pattern:** Abstraction over data access (future)
- **Service Layer Pattern:** Business logic separation from controllers

---

# 2. Functional Requirements

## 2.1 Query Execution Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-001 | Natural Language Query Input | High | User can submit natural language questions via API/SDK/Widget |
| FR-002 | SQL Generation from Natural Language | High | System converts natural language to valid SQL using GPT-4 with >90% accuracy |
| FR-003 | Streaming Query Results | High | System streams results via NDJSON with progress events (log, sql_generated, execution_progress, final_result) |
| FR-004 | Non-Streaming Query Execution | Medium | System supports synchronous query execution returning complete JSON response |
| FR-005 | Query Result Visualization | High | System automatically recommends visualization type (table/chart/PDF) based on data structure |
| FR-006 | Query History Tracking | Medium | System records all queries with metadata (query text, SQL, results, timestamp, token usage) |
| FR-007 | Schema Discovery | High | System dynamically discovers and caches database schema for accurate SQL generation |
| FR-008 | Multi-Database Support | High | System supports SQL Server, MySQL, PostgreSQL, SQLite, and Excel as data sources |

## 2.2 Tenant Management Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-009 | Tenant Registration | High | Admin can create new tenant with name, API key, connection string, database type |
| FR-010 | Tenant Configuration | High | Admin can configure tenant branding (logo URL, theme color) |
| FR-011 | Tenant Database Isolation | Critical | Each tenant connects to separate database with zero cross-tenant data access |
| FR-012 | Tenant API Key Management | High | Each tenant has unique API key for authentication |
| FR-013 | Tenant Activation/Deactivation | Medium | Admin can activate or deactivate tenants |
| FR-014 | Tenant CRUD Operations | High | System provides full CRUD for tenant management |

## 2.3 Security & Validation Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-015 | SELECT-Only Enforcement | Critical | System blocks all non-SELECT queries (INSERT, UPDATE, DELETE, DROP, ALTER) |
| FR-016 | SQL Injection Prevention | Critical | System detects and blocks SQL injection patterns (--;/*;UNION;@@;xp_) |
| FR-017 | Row Limit Enforcement | High | System automatically injects TOP/LIMIT 100 clause to prevent large result sets |
| FR-018 | Query Timeout Enforcement | High | System enforces 30-second query timeout |
| FR-019 | Rate Limiting | High | System enforces 60 requests/minute and 1000 requests/hour per tenant |
| FR-020 | API Key Authentication | Critical | All API requests require valid x-api-key header |

## 2.4 Reporting Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-021 | PDF Report Generation | Medium | System generates branded PDF reports with query results |
| FR-022 | Report Branding | Medium | PDFs include tenant logo and theme colors |
| FR-023 | Report Metadata | Low | PDFs include query text, execution time, timestamp |
| FR-024 | Table Rendering in PDF | Medium | PDFs render tabular data with alternating row colors |
| FR-025 | Chart Summary in PDF | Low | PDFs include chart data summaries when applicable |

## 2.5 Analytics & Monitoring Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-026 | Token Usage Tracking | High | System tracks OpenAI token consumption (prompt, completion, total) per query |
| FR-027 | Token Cost Estimation | Medium | System estimates cost based on model pricing (e.g., $0.03/1K tokens) |
| FR-028 | Usage Analytics by Tenant | High | System provides token usage summary per tenant with date filters |
| FR-029 | Conversation History | Medium | System stores conversation history with query, SQL, results, visualization type |
| FR-030 | Saved Analyses | Low | Users can save and reuse frequently used queries |

## 2.6 Integration Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-031 | REST API Endpoints | Critical | System exposes RESTful API for all operations |
| FR-032 | JavaScript SDK | High | System provides JavaScript SDK for easy web integration |
| FR-033 | Embeddable Widget | High | System provides plug-and-play widget with Shadow DOM isolation |
| FR-034 | CORS Support | High | System supports CORS for cross-origin requests from widgets |
| FR-035 | API Documentation | Medium | System provides Swagger/OpenAPI documentation |

---

# 3. Non-Functional Requirements

## 3.1 Performance

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| NFR-001 | Query Response Time (Streaming) | <5 seconds for first event | P95 latency |
| NFR-002 | Query Response Time (Non-Streaming) | <8 seconds total | P95 latency |
| NFR-003 | AI SQL Generation Time | <3 seconds | P95 latency |
| NFR-004 | Database Query Execution | <2 seconds for 100 rows | P95 latency |
| NFR-005 | API Throughput | 1000 requests/second | Peak load |
| NFR-006 | Widget Load Time | <2 seconds initial load | P95 latency |
| NFR-007 | Schema Cache Hit Rate | >90% | Monitoring metric |

## 3.2 Scalability

| ID | Requirement | Target | Notes |
|----|-------------|--------|-------|
| NFR-008 | Concurrent Tenants | 100+ active tenants | Initial target |
| NFR-009 | Concurrent Users per Tenant | 50+ simultaneous queries | Per tenant |
| NFR-010 | Database Connections | 100 max pool size per tenant | Connection pooling |
| NFR-011 | Horizontal Scaling | Support multi-instance deployment | Azure App Service scale-out |
| NFR-012 | Query Result Size | Up to 10,000 rows | With pagination |

## 3.3 Availability

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| NFR-013 | System Uptime | 99.5% (43.8 hours downtime/year) | Monthly SLA |
| NFR-014 | Planned Maintenance Window | <4 hours/month | Off-peak hours |
| NFR-015 | Database Availability | 99.9% | SQL Azure SLA |
| NFR-016 | Azure OpenAI Availability | 99.9% | Azure SLA |

## 3.4 Security

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-017 | Data Encryption in Transit | TLS 1.2+ for all API communication |
| NFR-018 | Data Encryption at Rest | Transparent Data Encryption (TDE) for databases |
| NFR-019 | API Key Storage | Hashed API keys (future: encrypted with AES-256) |
| NFR-020 | Connection String Security | Stored in Azure Key Vault (production) |
| NFR-021 | SQL Injection Protection | Multi-layer validation (keywords, patterns, AST) |
| NFR-022 | OWASP Top 10 Compliance | Mitigations for all OWASP Top 10 vulnerabilities |
| NFR-023 | Audit Logging | All queries logged with tenant ID, timestamp, user context |

## 3.5 Reliability

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-024 | Error Handling | Global exception middleware with sanitized error responses |
| NFR-025 | Circuit Breaker | Retry logic for transient failures (OpenAI API, DB) |
| NFR-026 | Graceful Degradation | Fallback to OpenAI API if Azure OpenAI unavailable |
| NFR-027 | Data Consistency | Transaction support for multi-table operations (future) |

## 3.6 Maintainability

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-028 | Code Coverage | >80% unit test coverage |
| NFR-029 | Code Quality | SonarQube quality gate pass (A rating) |
| NFR-030 | Documentation | Inline XML documentation for all public APIs |
| NFR-031 | Dependency Management | Automated dependency updates via Dependabot |
| NFR-032 | Technical Debt | <5% technical debt ratio |

## 3.7 Logging and Monitoring

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-033 | Structured Logging | Serilog with JSON formatting |
| NFR-034 | Log Levels | Debug, Info, Warning, Error, Critical |
| NFR-035 | Log Retention | 30 days rolling logs |
| NFR-036 | Application Insights | Real-time telemetry and distributed tracing |
| NFR-037 | Correlation IDs | Track requests across service boundaries |
| NFR-038 | Performance Metrics | Request rate, latency percentiles, error rate |

## 3.8 Accessibility

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-039 | Widget WCAG Compliance | WCAG 2.1 Level AA for widget UI |
| NFR-040 | Keyboard Navigation | Full keyboard support in widget |
| NFR-041 | Screen Reader Support | ARIA labels and semantic HTML |

## 3.9 Compliance

| ID | Requirement | Details |
|----|-------------|---------|
| NFR-042 | GDPR Compliance | Data subject rights (access, delete, export) |
| NFR-043 | Data Residency | Support for regional data storage requirements |
| NFR-044 | SOC 2 Type II | Audit-ready controls for security and availability |

---

# 4. Solution Architecture

## 4.1 Architecture Diagram Description

The AIQueryPlatform follows a **5-Layer Layered Architecture**:

```
┌───────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                            │
├───────────────────────────────────────────────────────────────────┤
│  • React Widget (Shadow DOM) - Embeddable UI component           │
│  • JavaScript SDK (AIQueryUI) - Programmatic API client          │
│  • Direct REST API Clients - Any HTTP client                     │
│                                                                   │
│  Features: Real-time streaming, Chart rendering, Form validation │
└───────────────────────────┬───────────────────────────────────────┘
                            │ HTTPS/JSON/NDJSON
┌───────────────────────────▼───────────────────────────────────────┐
│                     API GATEWAY LAYER                             │
├───────────────────────────────────────────────────────────────────┤
│  Middleware Pipeline (Order matters):                             │
│  [1] ExceptionHandlingMiddleware - Global error handling         │
│  [2] TenantResolutionMiddleware - x-api-key → TenantContext      │
│  [3] RateLimitingMiddleware - Quota enforcement                  │
│  [4] CORS Policy - Cross-origin support                          │
│  [5] Request Logging - Serilog HTTP logging                      │
│                                                                   │
│  Responsibilities: Auth, Rate limiting, Error handling, CORS      │
└───────────────────────────┬───────────────────────────────────────┘
                            │
┌───────────────────────────▼───────────────────────────────────────┐
│                     CONTROLLERS LAYER                             │
├───────────────────────────────────────────────────────────────────┤
│  • QueryController - Query execution (streaming/sync), PDF, schema│
│  • TenantController - Tenant CRUD operations                     │
│  • ConversationsController - Query history management            │
│  • AnalysesController - Saved analyses CRUD                      │
│  • TokenUsageController - Usage analytics                        │
│                                                                   │
│  Responsibilities: Request validation, Response formatting        │
└───────────────────────────┬───────────────────────────────────────┘
                            │
┌───────────────────────────▼───────────────────────────────────────┐
│                     SERVICES LAYER (Business Logic)               │
├───────────────────────────────────────────────────────────────────┤
│  Core Services:                                                   │
│  • QueryOrchestrationService - Pipeline coordination              │
│  • NLToSqlService - Azure OpenAI integration                     │
│  • SqlValidatorService - Security validation                     │
│  • QueryExecutionService - Database execution                    │
│  • IntelligenceLayerService - Visualization AI                   │
│  • SchemaService - Schema discovery & caching                    │
│  • ReportingService - PDF generation                             │
│                                                                   │
│  Supporting Services:                                             │
│  • TenantService - Tenant management                             │
│  • ConversationService - History tracking                        │
│  • TokenUsageService - Token analytics                           │
│                                                                   │
│  Responsibilities: Business rules, Orchestration, AI integration  │
└───────────────────────────┬───────────────────────────────────────┘
                            │
┌───────────────────────────▼───────────────────────────────────────┐
│                     DATA ACCESS LAYER                             │
├───────────────────────────────────────────────────────────────────┤
│  Platform Database (AIQueryPlatform):                             │
│  • Tenants - Tenant configuration                                │
│  • Conversations - Query history                                 │
│  • SavedAnalyses - Saved queries                                 │
│  • TokenUsage - Token consumption                                │
│                                                                   │
│  Tenant Databases (Isolated per tenant):                         │
│  • Business data (schema varies)                                 │
│  • Discovered via INFORMATION_SCHEMA                             │
│                                                                   │
│  Memory Cache:                                                    │
│  • Schema cache (1-hour TTL)                                     │
│  • Rate limit counters                                           │
│                                                                   │
│  External Services:                                               │
│  • Azure OpenAI (GPT-4) - Primary AI provider                    │
│  • OpenAI API - Fallback provider                                │
│                                                                   │
│  Responsibilities: Data persistence, Schema discovery, Caching    │
└───────────────────────────────────────────────────────────────────┘
```

## 4.2 Frontend Architecture

### 4.2.1 Widget Architecture (React + Shadow DOM)

```
┌─────────────────────────────────────────────────────────────┐
│                    Host Web Page                            │
│                                                             │
│  <script src="ai-widget.js"                                │
│          data-api-url="..."                                │
│          data-api-key="..."></script>                      │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │             Shadow DOM (Isolated)                     │ │
│  ├───────────────────────────────────────────────────────┤ │
│  │  ┌─────────────────────────────────────────────────┐ │ │
│  │  │  React App (Widget)                             │ │ │
│  │  ├─────────────────────────────────────────────────┤ │ │
│  │  │  Components:                                    │ │ │
│  │  │  • FloatingButton - Entry point                │ │ │
│  │  │  • WidgetPanel - Main container                │ │ │
│  │  │  • QueryBar - Input field                      │ │ │
│  │  │  • ResultsTable - Table renderer               │ │ │
│  │  │  • ChartRenderer - Chart.js wrapper            │ │ │
│  │  │  • InsightCard - AI insight display            │ │ │
│  │  │  • SuggestedQueries - Quick queries            │ │ │
│  │  │                                                 │ │ │
│  │  │  Services:                                      │ │ │
│  │  │  • APIService - Fetch wrapper                  │ │ │
│  │  │  • StreamingService - NDJSON parser            │ │ │
│  │  │                                                 │ │ │
│  │  │  State: React useState/useContext              │ │ │
│  │  └─────────────────────────────────────────────────┘ │ │
│  │                                                       │ │
│  │  Scoped Styles (no leakage to host)                  │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 4.2.2 JavaScript SDK Architecture

```typescript
// High-level SDK interface
class AIQueryUI {
  constructor(config: Config)
  async executeQuery(query: string): Promise<QueryResponse>
  async executeQueryStreaming(query: string): Promise<void>
  async generateReport(query: string): Promise<Blob>
  async getSchema(): Promise<SchemaResponse>
  
  // Event hooks
  onLog(callback: (message: string) => void)
  onError(callback: (error: string) => void)
  onResult(callback: (result: QueryResponse) => void)
}
```

## 4.3 Backend Architecture

### 4.3.1 Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                  AIQueryPlatform.Api                        │
│  • Controllers (QueryController, TenantController, etc.)    │
│  • Middleware (ExceptionHandling, TenantResolution, etc.)   │
│  • Program.cs (Startup & DI configuration)                  │
│  • Models/DTOs (Request/Response contracts)                 │
└─────────────────────────────┬───────────────────────────────┘
                              │ depends on
┌─────────────────────────────▼───────────────────────────────┐
│              AIQueryPlatform.LLMService                     │
│  • LLMServicePipe (OpenAI client abstraction)               │
│  • PromptBuilders (SQL Server, MySQL, PostgreSQL, etc.)    │
│  • Models (OpenAI request/response DTOs)                    │
└─────────────────────────────┬───────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────┐
│           AIQueryPlatform.SqlValidator                      │
│  • Validators (Keyword, Pattern, Statement validators)      │
│  • Models (ValidationResult)                                │
└─────────────────────────────────────────────────────────────┘
```

## 4.4 Database Architecture

### 4.4.1 Multi-Database Topology

```
┌─────────────────────────────────────────────────────────────┐
│              Platform Database (AIQueryPlatform)            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Tables:                                              │  │
│  │  • Tenants (configuration, API keys, connections)     │  │
│  │  • Conversations (query history with metadata)        │  │
│  │  • SavedAnalyses (reusable query templates)           │  │
│  │  • TokenUsage (OpenAI consumption tracking)           │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Stores connection strings to:
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Tenant Databases (Isolated)                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ Tenant A DB  │  │ Tenant B DB  │  │  Tenant C DB     │  │
│  │ (SQL Server) │  │ (MySQL)      │  │  (PostgreSQL)    │  │
│  ├──────────────┤  ├──────────────┤  ├──────────────────┤  │
│  │ • Customers  │  │ • Orders     │  │  • Products      │  │
│  │ • Orders     │  │ • Products   │  │  • Sales         │  │
│  │ • Products   │  │ • Users      │  │  • Inventory     │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 4.4.2 Database Provider Strategy

The system supports multiple database providers through an abstraction layer:

```csharp
public interface IDatabaseExecutor
{
    Task<QueryResult> ExecuteQueryAsync(string sql);
    Task<SchemaResponse> GetSchemaAsync();
}

// Implementations:
// - SqlServerExecutor (Microsoft.Data.SqlClient)
// - MySqlExecutor (MySqlConnector)
// - PostgreSqlExecutor (Npgsql)
// - SqliteExecutor (Microsoft.Data.Sqlite)
// - ExcelExecutor (System.Data.OleDb)
```

## 4.5 Integration Architecture

### 4.5.1 Azure OpenAI Integration

```
┌──────────────────┐
│  NLToSqlService  │
└────────┬─────────┘
         │
         ▼
┌─────────────────────────────┐
│   LLMServicePipe            │
│   (OpenAI Client Wrapper)   │
└────────┬────────────────────┘
         │
         ├─► [Primary] Azure OpenAI Endpoint
         │   • Endpoint: custom Azure instance
         │   • Model: GPT-4 or gpt-4o
         │   • Deployment: tenant-specific
         │   • API Key: from Key Vault
         │
         └─► [Fallback] OpenAI API
             • Endpoint: api.openai.com
             • Model: gpt-4
             • API Key: from Key Vault
```

### 4.5.2 External Service Dependencies

| Service | Purpose | Failure Impact | Mitigation |
|---------|---------|---------------|-----------|
| **Azure OpenAI** | NL2SQL conversion | Core feature unavailable | Fallback to OpenAI API, circuit breaker |
| **OpenAI API** | Fallback AI provider | Secondary failure, no SQL generation | Error message to user, retry logic |
| **Tenant Databases** | Data source | Query execution fails for tenant | Connection retry, error logging |
| **Application Insights** | Monitoring/telemetry | Observability reduced | Serilog file backup, no business impact |

---

# 5. Frontend Technical Design (React)

## 5.1 Application Structure

### Widget Project Structure
```
widget/
├── public/
│   ├── example.html         # Integration examples
│   ├── test.html            # Widget test page
│   └── widget.html          # Standalone widget page
├── src/
│   ├── index.tsx            # React entry point
│   ├── loader.ts            # Vanilla JS loader (5KB)
│   ├── components/          # React components
│   │   ├── FloatingButton.tsx
│   │   ├── WidgetPanel.tsx
│   │   ├── QueryBar.tsx
│   │   ├── ResultsTable.tsx
│   │   ├── ChartRenderer.tsx
│   │   └── InsightCard.tsx
│   ├── services/
│   │   ├── api.ts           # API client
│   │   └── streaming.ts     # NDJSON parser
│   ├── state/
│   │   └── WidgetContext.tsx  # React Context
│   ├── styles/
│   │   ├── widget.css       # Scoped styles
│   │   └── themes.css       # Light/dark themes
│   ├── types/
│   │   └── index.ts         # TypeScript definitions
│   └── utils/
│       ├── formatters.ts    # Data formatting
│       └── validators.ts    # Input validation
├── dist/                    # Build output
│   ├── ai-widget.js         # Loader script (5KB)
│   ├── widget-app.js        # React bundle
│   └── vendors.js           # Third-party libraries
├── package.json
├── tsconfig.json
└── webpack.config.js
```

### SDK Project Structure
```
sdk/
├── src/
│   ├── AIQueryUI.ts         # Main SDK class
│   ├── types.ts             # TypeScript interfaces
│   └── utils.ts             # Helper functions
├── dist/
│   ├── ai-query-sdk.js      # UMD bundle
│   ├── ai-query-sdk.esm.js  # ES Module
│   └── ai-query-sdk.d.ts    # TypeScript definitions
└── package.json
```

## 5.2 Routing Strategy

The widget is a single-page application without traditional routing. State management handles view transitions:

```typescript
enum WidgetView {
  COLLAPSED = 'collapsed',     // Floating button only
  EXPANDED = 'expanded',       // Panel open
  QUERY_INPUT = 'query_input', // User typing
  LOADING = 'loading',         // Query executing
  RESULTS = 'results',         // Displaying results
  ERROR = 'error'              // Error state
}
```

**Navigation Flow:**
1. User clicks floating button → `EXPANDED`
2. User enters query → `QUERY_INPUT`
3. User submits → `LOADING`
4. Results received → `RESULTS`
5. Error occurs → `ERROR`

## 5.3 State Management

### React Context API (No Redux needed for widget simplicity)

```typescript
interface WidgetState {
  // UI State
  view: WidgetView;
  isExpanded: boolean;
  theme: 'light' | 'dark';
  
  // Query State
  currentQuery: string;
  queryHistory: QueryHistoryItem[];
  
  // Results State
  results: QueryResponse | null;
  isStreaming: boolean;
  streamingLogs: string[];
  
  // Error State
  error: string | null;
  
  // Config
  apiUrl: string;
  apiKey: string;
  tenantId: string;
}

interface WidgetActions {
  setView(view: WidgetView): void;
  toggleExpanded(): void;
  setTheme(theme: 'light' | 'dark'): void;
  executeQuery(query: string): Promise<void>;
  clearResults(): void;
  setError(error: string): void;
}
```

**Context Provider:**
```typescript
export const WidgetProvider: React.FC<Props> = ({ children, config }) => {
  const [state, setState] = useState<WidgetState>(initialState);
  
  const actions: WidgetActions = {
    executeQuery: async (query) => {
      setState(prev => ({ ...prev, view: 'loading', currentQuery: query }));
      try {
        const response = await apiService.executeQuery(query);
        setState(prev => ({ ...prev, view: 'results', results: response }));
      } catch (error) {
        setState(prev => ({ ...prev, view: 'error', error: error.message }));
      }
    },
    // ... other actions
  };
  
  return (
    <WidgetContext.Provider value={{ state, actions }}>
      {children}
    </WidgetContext.Provider>
  );
};
```

## 5.4 API Integration

### API Service Layer

```typescript
class APIService {
  private baseUrl: string;
  private apiKey: string;
  
  constructor(config: { apiUrl: string; apiKey: string }) {
    this.baseUrl = config.apiUrl;
    this.apiKey = config.apiKey;
  }
  
  async executeQuery(query: string): Promise<QueryResponse> {
    const response = await fetch(`${this.baseUrl}/api/query/execute`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': this.apiKey
      },
      body: JSON.stringify({ query })
    });
    
    if (!response.ok) {
      throw new Error(await this.handleError(response));
    }
    
    return response.json();
  }
  
  async *executeQueryStreaming(query: string): AsyncGenerator<StreamEvent> {
    const response = await fetch(`${this.baseUrl}/api/query/execute-stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': this.apiKey
      },
      body: JSON.stringify({ query })
    });
    
    const reader = response.body!.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';
      
      for (const line of lines) {
        if (line.trim()) {
          yield JSON.parse(line) as StreamEvent;
        }
      }
    }
  }
  
  private async handleError(response: Response): Promise<string> {
    try {
      const error = await response.json();
      return error.message || error.error || 'Unknown error';
    } catch {
      return `HTTP ${response.status}: ${response.statusText}`;
    }
  }
}
```

### Error Handling

```typescript
// Retry logic with exponential backoff
async function fetchWithRetry(
  url: string,
  options: RequestInit,
  maxRetries: number = 3
): Promise<Response> {
  for (let i = 0; i < maxRetries; i++) {
    try {
      const response = await fetch(url, options);
      if (response.ok || response.status < 500) {
        return response;
      }
    } catch (error) {
      if (i === maxRetries - 1) throw error;
    }
    await new Promise(resolve => setTimeout(resolve, Math.pow(2, i) * 1000));
  }
  throw new Error('Max retries exceeded');
}
```

### Token Refresh (Future Enhancement)

Currently uses static API keys. JWT-based auth would require:

```typescript
class AuthService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  
  async refreshAccessToken(): Promise<string> {
    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: this.refreshToken })
    });
    const data = await response.json();
    this.accessToken = data.accessToken;
    return this.accessToken;
  }
}
```

## 5.5 UI Components

### Component Specifications

#### FloatingButton Component
```typescript
interface FloatingButtonProps {
  onClick: () => void;
  isExpanded: boolean;
  theme: 'light' | 'dark';
}

// State: None (controlled by parent)
// Events: onClick
// Styling: Fixed position bottom-right, z-index 9999
```

#### QueryBar Component
```typescript
interface QueryBarProps {
  value: string;
  onChange: (value: string) => void;
  onSubmit: (query: string) => void;
  disabled: boolean;
  placeholder?: string;
}

// State: Internal focus state
// Events: onChange, onSubmit, onKeyDown (Enter key)
// Validation: Min 3 characters, max 500 characters
```

#### ResultsTable Component
```typescript
interface ResultsTableProps {
  columns: string[];
  rows: Record<string, any>[];
  maxRows?: number; // Default 100
}

// State: Sorting state, pagination state
// Features: Sortable columns, pagination, CSV export
```

#### ChartRenderer Component
```typescript
interface ChartRendererProps {
  data: {
    labels: string[];
    values: number[];
  };
  type: 'bar' | 'line' | 'pie';
}

// Dependencies: Chart.js 4.x
// State: Chart instance reference
// Features: Responsive, tooltips, legend
```

---

# 6. Backend Technical Design (.NET 8)

## 6.1 Project Structure

```
src/
├── AIQueryPlatform.Api/                 # Web API Project
│   ├── Controllers/
│   │   ├── QueryController.cs           # Query execution endpoints
│   │   ├── TenantController.cs          # Tenant management
│   │   ├── ConversationsController.cs   # Query history
│   │   ├── AnalysesController.cs        # Saved analyses
│   │   └── TokenUsageController.cs      # Usage analytics
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── TenantResolutionMiddleware.cs
│   │   ├── RateLimitingMiddleware.cs
│   │   └── QuotaEnforcementMiddleware.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IQueryOrchestrationService.cs
│   │   │   ├── INLToSqlService.cs
│   │   │   ├── ISqlValidatorService.cs
│   │   │   ├── IQueryExecutionService.cs
│   │   │   ├── ISchemaService.cs
│   │   │   ├── IIntelligenceLayerService.cs
│   │   │   ├── IReportingService.cs
│   │   │   ├── ITenantService.cs
│   │   │   ├── IConversationService.cs
│   │   │   └── ITokenUsageService.cs
│   │   ├── QueryOrchestrationService.cs
│   │   ├── NLToSqlService.cs
│   │   ├── SqlValidatorService.cs
│   │   ├── QueryExecutionService.cs
│   │   ├── SchemaService.cs
│   │   ├── IntelligenceLayerService.cs
│   │   ├── ReportingService.cs
│   │   ├── TenantService.cs
│   │   ├── ConversationService.cs
│   │   ├── TokenUsageService.cs
│   │   ├── Executors/              # Database-specific executors
│   │   │   ├── SqlServerExecutor.cs
│   │   │   ├── MySqlExecutor.cs
│   │   │   ├── PostgreSqlExecutor.cs
│   │   │   ├── SqliteExecutor.cs
│   │   │   └── ExcelExecutor.cs
│   │   └── PromptBuilders/         # Database-specific prompts
│   │       ├── SqlServerPromptBuilder.cs
│   │       ├── MySqlPromptBuilder.cs
│   │       └── PostgreSqlPromptBuilder.cs
│   ├── Models/
│   │   ├── Tenant.cs
│   │   ├── TenantContext.cs
│   │   ├── Conversation.cs
│   │   ├── SavedAnalysis.cs
│   │   ├── TokenUsage.cs
│   │   ├── RateLimitInfo.cs
│   │   └── DTOs/
│   │       ├── QueryRequest.cs
│   │       ├── QueryResponse.cs
│   │       ├── StreamEvent.cs
│   │       ├── SchemaResponse.cs
│   │       └── ValidationResult.cs
│   ├── Helpers/
│   │   └── OpenAITokenExtensions.cs  # Token counting utilities
│   ├── Program.cs                     # Application startup
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Local.json         # Local secrets (gitignored)
├── AIQueryPlatform.LLMService/        # LLM Integration Library
│   ├── LLMServicePipe.cs              # OpenAI client wrapper
│   ├── Interface/
│   │   └── ILLMServicePipe.cs
│   ├── Models/
│   │   ├── OpenAISettings.cs
│   │   ├── LLMRequest.cs
│   │   └── LLMResponse.cs
│   ├── Services/
│   │   └── TokenCountingService.cs
│   └── Tools/
│       └── PromptTemplates.cs
└── AIQueryPlatform.SqlValidator/      # SQL Validation Library
    ├── Validators/
    │   ├── KeywordValidator.cs
    │   ├── PatternValidator.cs
    │   └── StatementValidator.cs
    └── Services/
        └── SqlSanitizerService.cs
```

## 6.2 API Design

```
┌─────────────────────────────────────────────────────────────────┐
│                        External Systems                         │
├─────────────────────────────────────────────────────────────────┤
│  Azure OpenAI │ OpenAI API │ SQL Server │ MySQL │ PostgreSQL   │
└────────────┬──────────────────────────┬──────────────────────────┘
             │                          │
             │                          │
┌────────────▼──────────────────────────▼──────────────────────────┐
│                    AI Query Platform                             │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              API Gateway Layer                             │  │
│  │  • Tenant Resolution (x-api-key)                          │  │
│  │  • Rate Limiting (per-tenant quotas)                      │  │
│  │  • Exception Handling                                     │  │
│  │  • CORS Policy                                            │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              Controllers Layer                             │  │
│  │  • QueryController (streaming/non-streaming)              │  │
│  │  • TenantController (management)                          │  │
│  │  • ConversationsController (history)                      │  │
│  │  • AnalysesController (saved queries)                     │  │
│  │  • TokenUsageController (analytics)                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              Services Layer                                │  │
│  │  • QueryOrchestrationService (pipeline coordinator)       │  │
│  │  • NLToSqlService (AI integration)                        │  │
│  │  • SqlValidatorService (security)                         │  │
│  │  • QueryExecutionService (SQL execution)                  │  │
│  │  • IntelligenceLayerService (visualization AI)            │  │
│  │  • ReportingService (PDF generation)                      │  │
│  │  • SchemaService (metadata discovery)                     │  │
│  │  • TenantService (tenant management)                      │  │
│  │  • ConversationService (history tracking)                 │  │
│  │  • TokenUsageService (usage analytics)                    │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              Data Access Layer                             │  │
│  │  • Platform Database (tenants, conversations, tokens)     │  │
│  │  • Tenant Databases (per-tenant isolated data)            │  │
│  │  • Memory Cache (schema, rate limits)                     │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               │
┌──────────────────────────────▼───────────────────────────────────┐
│                        Client Applications                       │
├──────────────────────────────────────────────────────────────────┤
│  JavaScript SDK │ Embeddable Widget │ Custom Integrations       │
└──────────────────────────────────────────────────────────────────┘
```

### 2.2 High-Level Architecture

**Architecture Pattern:** Layered Architecture with Clean Architecture principles

**Technology Stack:**
- **Backend:** .NET 8 (C#), ASP.NET Core Web API
- **AI/LLM:** Azure OpenAI GPT-4, OpenAI API
- **Databases:** SQL Server (platform + tenants), MySQL, PostgreSQL, SQLite, Excel
- **Caching:** In-Memory Cache (IMemoryCache)
- **Logging:** Serilog with file and console sinks
- **PDF Generation:** iTextSharp.LGPLv2.Core
- **Frontend SDK:** TypeScript, React 18, Webpack 5
- **Visualization:** Chart.js 4.x
- **Containerization:** Docker with docker-compose

**Key Architectural Principles:**
1. **Multi-Tenancy:** Complete isolation at database and request level
2. **Security First:** Validation, rate limiting, and SELECT-only enforcement
3. **Streaming:** IAsyncEnumerable for real-time feedback
4. **Separation of Concerns:** Layered architecture with clear boundaries
5. **Dependency Injection:** All services registered and injected
6. **Configuration-Based:** Settings externalized via appsettings.json

### 2.3 Key Components

- **API Gateway Layer**: Request authentication, tenant resolution, rate limiting, CORS
- **Controllers**: HTTP endpoint handlers for queries, tenants, conversations, analyses, token usage
- **Orchestration Service**: Pipeline coordinator managing the query execution flow
- **NL2SQL Service**: Azure OpenAI integration for natural language to SQL conversion
- **SQL Validator**: Security layer blocking malicious queries and enforcing SELECT-only
- **Query Executor**: Secure SQL execution against tenant databases with timeouts
- **Intelligence Layer**: AI-powered visualization type detection (table/chart/PDF)
- **Schema Service**: Dynamic database schema discovery with caching
- **Reporting Service**: Branded PDF generation with tables and charts
- **Tenant Service**: Tenant CRUD operations and API key management
- **Conversation Service**: Query history tracking with metadata
- **Token Usage Service**: OpenAI token consumption tracking and analytics
- **JavaScript SDK**: Frontend library for easy integration
- **Embeddable Widget**: React-based widget with Shadow DOM isolation

---

## 3. Design Considerations

### 3.1 Assumptions

- **Azure OpenAI Availability**: Azure OpenAI service is available with sufficient quota
- **Database Access**: Tenant databases are accessible from the API server
- **Network Connectivity**: Clients can reach the API over HTTPS
- **Browser Support**: Modern browsers (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)
- **Query Complexity**: Most queries return < 10,000 rows (100-row default limit enforced)
- **API Key Security**: Tenants store API keys securely (not in client-side code)
- **Single Region**: Initial deployment targets single Azure region (scalable to multi-region)
- **English Language**: Primary support for English natural language queries

### 3.2 Constraints

**Technical Constraints:**
- **.NET 8 Runtime**: Requires .NET 8 SDK and runtime
- **SELECT-Only Queries**: No data modification allowed (enforced at validation layer)
- **Query Timeout**: 30-second maximum execution time per query
- **Row Limit**: Maximum 100 rows per query (configurable)
- **Rate Limits**: Per-tenant per-minute and per-hour limits
- **Token Limits**: Azure OpenAI token limits (varies by model)
- **PDF Size**: Limited by available memory (typical max ~50MB)

**Business Constraints:**
- **Multi-Tenant Isolation**: Absolute requirement - no cross-tenant data access
- **Cost Management**: OpenAI API costs must be trackable per tenant
- **Response Time**: Target < 5 seconds for typical queries (depends on DB and AI latency)
- **Uptime**: Target 99.5% availability (excludes planned maintenance)

**Regulatory/Compliance Constraints:**
- **Data Residency**: Tenant data must remain in tenant database (not cached)
- **Audit Trail**: All queries logged with timestamp and tenant ID
- **Secure Transport**: HTTPS required for all API communication

### 3.3 Dependencies

**External Services:**
- **Azure OpenAI Service** (or OpenAI API): NL2SQL conversion
  - Version: GPT-4 or gpt-3.5-turbo
  - Failure Impact: System cannot convert natural language to SQL
  - Mitigation: Fallback to OpenAI API, circuit breaker pattern

**Database Systems:**
- **SQL Server 2019+**: Platform and tenant databases
- **MySQL 8.0+**: Optional tenant databases
- **PostgreSQL 12+**: Optional tenant databases
- **SQLite 3.x**: Optional lightweight tenant databases

**Third-Party Libraries:**
- **Serilog 3.x**: Structured logging
- **iTextSharp.LGPLv2.Core 3.x**: PDF generation
- **System.Text.Json**: JSON serialization
- **Microsoft.Data.SqlClient**: SQL Server connectivity
- **MySqlConnector**: MySQL connectivity
- **Npgsql**: PostgreSQL connectivity

**Frontend Dependencies:**
- **React 18**: Widget UI framework
- **Chart.js 4.x**: Client-side visualizations
- **TypeScript 5.x**: Type-safe development

### 3.4 Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **OpenAI Service Outage** | High - Core feature unavailable | Medium | Circuit breaker, fallback to OpenAI API, cached query patterns |
| **SQL Injection** | Critical - Data breach | Low | Multi-layer validation, keyword filtering, parameterized queries |
| **Rate Limit Abuse** | Medium - Service degradation | Medium | Per-tenant rate limiting, quota enforcement, monitoring |
| **Database Connection Exhaustion** | High - Service unavailable | Medium | Connection pooling, timeout enforcement, connection limits |
| **Large Query Results** | Medium - Memory pressure | Medium | Row limits, pagination, streaming results |
| **Cross-Tenant Data Leak** | Critical - Security breach | Very Low | Scoped tenant context, integration tests, code reviews |
| **Token Cost Explosion** | Medium - Budget overrun | Medium | Token tracking, per-tenant quotas, cost alerts |
| **Schema Discovery Failure** | Medium - Inaccurate SQL | Low | Schema caching, fallback to basic schema, error handling |
| **PDF Generation Memory** | Medium - OOM crashes | Low | PDF size limits, streaming generation, memory monitoring |
| **CORS Misconfiguration** | Medium - Widget unusable | Low | Explicit origin allowlist, testing across domains |

---

## 4. Architectural Strategies

### 4.1 Strategy Selection

**Selected Architecture: Layered Architecture with Clean Architecture Principles**

**Rationale:**
1. **Clear Separation of Concerns**: Controllers, services, and data access are cleanly separated
2. **Testability**: Each layer can be unit tested independently with mocked dependencies
3. **Maintainability**: Changes in one layer have minimal impact on others
4. **Scalability**: Services can be horizontally scaled, cache layer added easily
5. **Team Familiarity**: Standard .NET architectural pattern well-understood by team
6. **Dependency Injection**: Native ASP.NET Core DI simplifies service registration

**Key Architectural Choices:**
- **Multi-Tenancy via Scoped Context**: TenantContext injected per-request, ensures isolation
- **Streaming with IAsyncEnumerable**: Native .NET 8 feature, efficient memory usage
- **Service-Oriented Services Layer**: Each service has single responsibility (SRP)
- **Middleware Pipeline**: Cross-cutting concerns (auth, rate limiting) handled uniformly
- **Schema Caching**: Balance between accuracy and performance (1-hour TTL)

### 4.2 Alternatives Considered

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| **Microservices Architecture** | Independent scaling, technology flexibility, fault isolation | Increased complexity, distributed tracing needed, network latency | **Rejected** - Overkill for initial scale, monolith easier to deploy and debug |
| **Event-Driven Architecture** | Loose coupling, async processing, scalability | Complex event schema management, eventual consistency challenges | **Rejected** - No strong requirement for async workflows, adds complexity |
| **CQRS Pattern** | Read/write separation, optimized queries | Increased code duplication, sync overhead | **Rejected** - Read-heavy workload doesn't justify write model separation |
| **GraphQL API** | Flexible queries, reduced overfetching | Learning curve, no streaming support out-of-box | **Rejected** - REST + NDJSON streaming simpler for our use case |
| **Shared Database Multi-Tenancy** | Single database, easier backup | Security risk, schema evolution complexity, noisy neighbor | **Rejected** - Separate databases required for compliance and isolation |
| **JWT Authentication** | Stateless, industry standard | Token refresh complexity, revocation challenges | **Rejected** - API keys simpler for machine-to-machine auth |
| **WebSockets for Streaming** | Full duplex, native browser support | Connection management, load balancer complexity | **Rejected** - NDJSON over HTTP simpler, works with standard load balancers |

### 4.3 Key Architectural Decisions

#### ADR-001: Multi-Tenancy Model - Database-per-Tenant
**Decision**: Each tenant has an isolated database with separate connection string  
**Rationale**:
- Strongest isolation guarantees (no risk of cross-tenant queries)
- Easy to backup, restore, or migrate individual tenants
- Schema can diverge per tenant if needed
- Compliance-friendly for data residency requirements

**Trade-offs**: Higher infrastructure cost, more complex schema migrations

---

#### ADR-002: Streaming via IAsyncEnumerable + NDJSON
**Decision**: Use .NET IAsyncEnumerable with NDJSON response format  
**Rationale**:
- Native .NET 8 support, no external libraries
- Memory-efficient streaming (yield return pattern)
- Client can display progress in real-time
- Standard NDJSON format, works with curl/fetch

**Trade-offs**: Requires HTTP/2 or chunked encoding, client must parse line-by-line

---

#### ADR-003: SELECT-Only Enforcement
**Decision**: Block all non-SELECT queries at validation layer  
**Rationale**:
- Fundamental security requirement (read-only access)
- Multiple layers: keyword filtering, statement parsing, regex validation
- Prevents accidental or malicious data modification

**Trade-offs**: Cannot support legitimate data modification use cases (by design)

---

#### ADR-004: AI Provider - Azure OpenAI with OpenAI Fallback
**Decision**: Primary Azure OpenAI, fallback to OpenAI API  
**Rationale**:
- Azure OpenAI provides enterprise SLA, data residency, VNet integration
- OpenAI API as fallback for availability and cost optimization
- Abstracted via LLMServiceOperator for provider-agnostic interface

**Trade-offs**: Dual provider management, potential consistency differences

---

#### ADR-005: Schema Discovery with Caching
**Decision**: Dynamic schema discovery via INFORMATION_SCHEMA with 1-hour cache  
**Rationale**:
- Supports schema evolution without code changes
- Caching reduces DB round-trips (schema rarely changes)
- INFORMATION_SCHEMA is database-agnostic (SQL Server, MySQL, PostgreSQL)

**Trade-offs**: Cached schema may be stale, need manual refresh mechanism

---

#### ADR-006: JavaScript SDK with Shadow DOM Widget
**Decision**: Provide both JavaScript SDK and embeddable widget with Shadow DOM  
**Rationale**:
- SDK for custom integrations (React, Angular, Vue)
- Widget for plug-and-play embedding (WordPress, static sites)
- Shadow DOM prevents CSS/JS conflicts with host page
- Lazy loading (5KB loader) for performance

**Trade-offs**: Shadow DOM limits styling flexibility, browser compatibility required

---

#### ADR-007: Rate Limiting In-Memory
**Decision**: In-memory rate limiting with sliding window per tenant  
**Rationale**:
- Simple implementation, no external store required
- Sufficient for single-instance or sticky session deployments
- Per-tenant limits prevent abuse

**Trade-offs**: Not distributed (doesn't work across load-balanced instances without sticky sessions)

**Future**: Migrate to Redis for distributed rate limiting

---

## 5. System Architecture

### 5.1 Component Diagram

```
┌───────────────────────────────────────────────────────────────────┐
│                         Client Layer                              │
├───────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐  │
│  │  JavaScript SDK  │  │ Embeddable Widget│  │ Custom Clients │  │
│  │  (AIQueryUI)     │  │  (Shadow DOM)    │  │  (REST API)    │  │
│  └──────────────────┘  └──────────────────┘  └────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
                                  │
                                  │ HTTPS
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                    Middleware Pipeline                            │
├───────────────────────────────────────────────────────────────────┤
│  [1] ExceptionHandlingMiddleware                                  │
│  [2] TenantResolutionMiddleware (x-api-key → TenantContext)       │
│  [3] RateLimitingMiddleware (per-tenant quotas)                   │
│  [4] Routing → Controllers                                        │
└───────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                       Controllers Layer                           │
├───────────────────────────────────────────────────────────────────┤
│  QueryController        │ TenantController    │ ConversationsCtrl │
│  • ExecuteStreamAsync   │ • GetAll            │ • GetByTenantId   │
│  • ExecuteAsync         │ • GetById           │ • Create          │
│  • GenerateReportAsync  │ • Create            │ • Update          │
│  • GetSchema            │ • Update            │ • Delete          │
│                         │ • Delete            │                   │
├─────────────────────────┼─────────────────────┼───────────────────┤
│  AnalysesController     │ TokenUsageController│                   │
│  • GetByTenantId        │ • GetUsageByTenantId│                   │
│  • Create               │ • GetUsageSummary   │                   │
│  • Delete               │                     │                   │
└───────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                       Services Layer                              │
├───────────────────────────────────────────────────────────────────┤
│  QueryOrchestrationService (Pipeline Coordinator)                 │
│  ├─► NLToSqlService (Azure OpenAI)                                │
│  ├─► SqlValidatorService (Security)                               │
│  ├─► QueryExecutionService (Tenant DB)                            │
│  └─► IntelligenceLayerService (Visualization AI)                  │
├───────────────────────────────────────────────────────────────────┤
│  SchemaService (INFORMATION_SCHEMA caching)                       │
│  ReportingService (PDF generation)                                │
│  TenantService (Tenant CRUD)                                      │
│  ConversationService (History tracking)                           │
│  TokenUsageService (Usage analytics)                              │
└───────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                       Data Layer                                  │
├───────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────┐  ┌──────────────────────────────┐   │
│  │   Platform Database     │  │   Tenant Databases           │   │
│  │   (AIQueryPlatform)     │  │   (per-tenant isolated)      │   │
│  ├─────────────────────────┤  ├──────────────────────────────┤   │
│  │ • Tenants               │  │ • Business Data (Customers,  │   │
│  │ • Conversations         │  │   Orders, Products, etc.)    │   │
│  │ • SavedAnalyses         │  │                              │   │
│  │ • TokenUsage            │  │ • Schema varies per tenant   │   │
│  └─────────────────────────┘  └──────────────────────────────┘   │
│  ┌─────────────────────────┐                                     │
│  │   Memory Cache          │                                     │
│  ├─────────────────────────┤                                     │
│  │ • Schema (TTL: 1h)      │                                     │
│  │ • Rate Limits           │                                     │
│  └─────────────────────────┘                                     │
└───────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                    External Services                              │
├───────────────────────────────────────────────────────────────────┤
│  Azure OpenAI (GPT-4)  │  OpenAI API (Fallback)                  │
└───────────────────────────────────────────────────────────────────┘
```

### 5.2 Data Flow

#### 5.2.1 Query Execution Flow (Streaming)

```
[Client] --1. POST /api/query/execute-stream + x-api-key-->
         [TenantResolutionMiddleware] --2. Resolve Tenant-->
         [RateLimitingMiddleware] --3. Check Quota-->
         [QueryController] --4. ExecuteStreamAsync-->
         [QueryOrchestrationService] --5. Orchestrate Pipeline-->
            |
            ├─► [NLToSqlService] --6a. Convert NL to SQL (OpenAI)-->
            |   ├─► [SchemaService] --6b. Get Schema (cached)-->
            |   └─► [LLMServicePipe] --6c. Call Azure OpenAI API-->
            |
            ├─► [SqlValidatorService] --7. Validate SQL (Security)-->
            |   ├─► Check Keywords (SELECT-only)
            |   ├─► Check Patterns (SQL Injection)
            |   └─► Inject Row Limit (TOP 100)
            |
            ├─► [QueryExecutionService] --8. Execute SQL (Tenant DB)-->
            |   └─► SqlConnection with Timeout
            |
            └─► [IntelligenceLayerService] --9. Determine Viz Type-->
                ├─► Analyze Result Structure
                └─► Return: table | chart | pdf
         
         [QueryController] --10. Yield NDJSON Events-->
         [Client] --11. Receive Streaming Response-->
```

**Stream Events:**
1. `{"type":"log","message":"Processing query..."}`
2. `{"type":"sql_generated","sql":"SELECT ..."}`
3. `{"type":"execution_progress","message":"Executing query..."}`
4. `{"type":"final_result","data":{...},"visualizationType":"table"}`
5. `{"type":"error","message":"..."}` (if error occurs)

#### 5.2.2 Schema Discovery Flow

```
[QueryController] --1. GET /api/query/schema-->
[SchemaService] --2. Check Memory Cache-->
   |
   ├─► CACHE HIT --3a. Return Cached Schema-->
   |
   └─► CACHE MISS --3b. Query INFORMATION_SCHEMA-->
       [QueryExecutionService] --4. Execute Schema Query-->
       [SchemaService] --5. Transform to SchemaResponse-->
       [MemoryCache] --6. Cache with 1h TTL-->
       [QueryController] --7. Return Schema-->
```

#### 5.2.3 PDF Report Generation Flow

```
[Client] --1. POST /api/query/generate-report-->
[QueryController] --2. GenerateReportAsync-->
[QueryOrchestrationService] --3. Execute Query Pipeline-->
   (same as Query Execution Flow above)
[ReportingService] --4. Generate PDF-->
   ├─► Load Tenant Branding (logo, theme)
   ├─► Create PDF Document (iTextSharp)
   ├─► Add Header with Logo
   ├─► Add Data Table
   └─► Add Metadata Footer
[QueryController] --5. Return PDF (application/pdf)-->
[Client] --6. Download PDF-->
```

### 5.3 API Design

#### 5.3.1 Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| **POST** | `/api/query/execute-stream` | Execute query with streaming response | x-api-key |
| **POST** | `/api/query/execute` | Execute query (non-streaming) | x-api-key |
| **POST** | `/api/query/generate-report` | Execute query and return PDF | x-api-key |
| **GET** | `/api/query/schema` | Get tenant database schema | x-api-key |
| **GET** | `/api/tenant` | Get all tenants | Admin |
| **GET** | `/api/tenant/{id}` | Get tenant by ID | Admin |
| **POST** | `/api/tenant` | Create new tenant | Admin |
| **PUT** | `/api/tenant/{id}` | Update tenant | Admin |
| **DELETE** | `/api/tenant/{id}` | Delete tenant | Admin |
| **GET** | `/api/conversations` | Get conversation history | x-api-key |
| **POST** | `/api/conversations` | Create conversation | x-api-key |
| **PUT** | `/api/conversations/{id}` | Update conversation | x-api-key |
| **DELETE** | `/api/conversations/{id}` | Delete conversation | x-api-key |
| **GET** | `/api/analyses` | Get saved analyses | x-api-key |
| **POST** | `/api/analyses` | Save analysis | x-api-key |
| **DELETE** | `/api/analyses/{id}` | Delete saved analysis | x-api-key |
| **GET** | `/api/tokenusage` | Get token usage by tenant | x-api-key |
| **GET** | `/api/tokenusage/summary` | Get usage summary | x-api-key |

#### 5.3.2 Request/Response Schemas

**Query Request (POST /api/query/execute-stream)**
```typescript
interface QueryRequest {
  query: string;              // Natural language query
  tenantId?: string;          // Optional tenant ID override
  context?: string;           // Optional additional context
  userRole?: string;          // Optional user role for prompt
  conversationId?: string;    // Optional conversation ID
}
```

**Query Response (POST /api/query/execute)**
```typescript
interface QueryResponse {
  success: boolean;
  data: {
    columns: string[];
    rows: Record<string, any>[];
    visualizationType: 'table' | 'chart' | 'pdf';
  };
  sql: string;
  executionTime: number;      // milliseconds
  timestamp: string;          // ISO 8601 UTC
  tokenUsage?: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
  };
}
```

**Stream Event (NDJSON)**
```typescript
interface StreamEvent {
  type: 'log' | 'sql_generated' | 'execution_progress' | 'final_result' | 'error';
  message?: string;
  sql?: string;
  data?: {
    columns: string[];
    rows: Record<string, any>[];
    visualizationType: 'table' | 'chart' | 'pdf';
  };
  timestamp: string;          // ISO 8601 UTC
  tokenUsage?: TokenUsage;
}
```

**Schema Response (GET /api/query/schema)**
```typescript
interface SchemaResponse {
  tables: Array<{
    name: string;
    columns: Array<{
      name: string;
      dataType: string;
      isNullable: boolean;
      isPrimaryKey: boolean;
    }>;
  }>;
  cachedAt: string;           // ISO 8601 UTC
}
```

**Tenant Model**
```typescript
interface Tenant {
  tenantId: string;           // GUID
  name: string;
  apiKey: string;             // Unique per tenant
  connectionString: string;   // Encrypted in production
  databaseType: DatabaseType; // 0=SqlServer, 1=MySql, 2=PostgreSql, 3=Excel, 4=Sqlite
  databaseSettings?: string;  // JSON for additional config
  logoUrl?: string;
  themeColor?: string;        // Hex color
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

enum DatabaseType {
  SqlServer = 0,
  MySql = 1,
  PostgreSql = 2,
  Excel = 3,
  Sqlite = 4
}
```

**Conversation Model**
```typescript
interface Conversation {
  conversationId: string;     // GUID
  tenantId: string;
  title: string;
  query: string;
  sqlGenerated: string;
  result: string;             // JSON serialized
  visualizationType: string;
  tokenUsage?: TokenUsage;
  createdAt: string;
  updatedAt?: string;
}
```

**Saved Analysis Model**
```typescript
interface SavedAnalysis {
  analysisId: string;         // GUID
  tenantId: string;
  name: string;
  description?: string;
  query: string;
  sqlTemplate: string;
  parameters?: string;        // JSON
  createdAt: string;
}
```

**Token Usage Model**
```typescript
interface TokenUsage {
  usageId: string;            // GUID
  tenantId: string;
  conversationId?: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  modelName: string;
  estimatedCost: number;
  createdAt: string;
}
```

### 5.4 Data Models

#### 5.4.1 Platform Database Schema (AIQueryPlatform)

**Tenants Table**
```sql
CREATE TABLE Tenants (
    TenantId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    ApiKey NVARCHAR(500) NOT NULL UNIQUE,
    ConnectionString NVARCHAR(1000) NOT NULL,
    DatabaseType INT NOT NULL DEFAULT 0,
    DatabaseSettings NVARCHAR(MAX) NULL,
    LogoUrl NVARCHAR(500) NULL,
    ThemeColor NVARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    INDEX IX_Tenants_ApiKey (ApiKey),
    INDEX IX_Tenants_IsActive (IsActive),
    INDEX IX_Tenants_DatabaseType (DatabaseType)
);
```

**Conversations Table**
```sql
CREATE TABLE Conversations (
    ConversationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(500) NOT NULL,
    Query NVARCHAR(MAX) NOT NULL,
    SqlGenerated NVARCHAR(MAX) NOT NULL,
    Result NVARCHAR(MAX) NULL,
    VisualizationType NVARCHAR(50) NULL,
    TokenUsageId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    INDEX IX_Conversations_TenantId (TenantId),
    INDEX IX_Conversations_CreatedAt (CreatedAt),
    CONSTRAINT FK_Conversations_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(TenantId) ON DELETE CASCADE
);
```

**SavedAnalyses Table**
```sql
CREATE TABLE SavedAnalyses (
    AnalysisId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Query NVARCHAR(MAX) NOT NULL,
    SqlTemplate NVARCHAR(MAX) NOT NULL,
    Parameters NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_SavedAnalyses_TenantId (TenantId),
    CONSTRAINT FK_SavedAnalyses_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(TenantId) ON DELETE CASCADE
);
```

**TokenUsage Table**
```sql
CREATE TABLE TokenUsage (
    UsageId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ConversationId UNIQUEIDENTIFIER NULL,
    PromptTokens INT NOT NULL,
    CompletionTokens INT NOT NULL,
    TotalTokens INT NOT NULL,
    ModelName NVARCHAR(100) NOT NULL,
    EstimatedCost DECIMAL(18, 6) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_TokenUsage_TenantId (TenantId),
    INDEX IX_TokenUsage_CreatedAt (CreatedAt),
    CONSTRAINT FK_TokenUsage_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(TenantId) ON DELETE CASCADE,
    CONSTRAINT FK_TokenUsage_Conversations FOREIGN KEY (ConversationId) 
        REFERENCES Conversations(ConversationId) ON DELETE SET NULL
);
```

#### 5.4.2 Tenant Database Schema (Variable)

Tenant databases contain business-specific tables (e.g., Customers, Orders, Products). Schema is discovered dynamically via INFORMATION_SCHEMA queries.

**Example Tenant Schema:**
```sql
-- DemoTenantDB
CREATE TABLE Customers (
    CustomerId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY,
    CustomerId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);

CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    Stock INT NOT NULL
);
```

---

## 6. Policies and Tactics

### 6.1 Security

#### 6.1.1 Authentication
- **API Key Based**: Each tenant has unique API key in `x-api-key` header
- **Key Storage**: API keys stored in platform database, encrypted at rest (future)
- **Key Rotation**: Manual key rotation via tenant update endpoint (automated rotation planned)
- **Admin Endpoints**: Protected by separate admin authentication (not implemented - future)

#### 6.1.2 Authorization
- **Tenant Isolation**: TenantContext scoped per request, prevents cross-tenant access
- **Database Isolation**: Each tenant connects to separate database
- **Schema Isolation**: Schema cache keyed by tenant ID

#### 6.1.3 SQL Injection Prevention
**Multi-Layer Defense:**
1. **Keyword Blacklist**: Block INSERT, UPDATE, DELETE, DROP, ALTER, EXEC, TRUNCATE, CREATE
2. **Pattern Detection**: Regex patterns for common injection vectors (--,/*,*/,;,UNION,@@)
3. **Statement Validation**: Ensure single SELECT statement only
4. **Comment Stripping**: Remove SQL comments before execution
5. **Row Limit Injection**: Auto-inject TOP/LIMIT clause

**Validation Service:**
```csharp
public class SqlValidatorService : ISqlValidatorService
{
    private static readonly string[] ForbiddenKeywords = {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "EXEC",
        "EXECUTE", "TRUNCATE", "CREATE", "GRANT", "REVOKE"
    };

    public async Task<ValidationResult> ValidateAsync(string sql)
    {
        // 1. Check forbidden keywords
        // 2. Check injection patterns
        // 3. Ensure single statement
        // 4. Inject row limit if missing
    }
}
```

#### 6.1.4 Data Encryption
- **In Transit**: HTTPS/TLS 1.2+ required for all API traffic
- **At Rest**: Database encryption (TDE) recommended for tenant databases
- **Connection Strings**: Stored in appsettings.json (Key Vault in production)
- **API Keys**: Plain text in DB (future: encrypted with AES-256)

#### 6.1.5 Rate Limiting
**Per-Tenant Quotas:**
- **Per Minute**: 60 requests (configurable)
- **Per Hour**: 1000 requests (configurable)

**Implementation:**
```csharp
public class RateLimitingMiddleware
{
    private readonly IMemoryCache _cache;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Items["TenantId"] as string;
        var key = $"ratelimit:{tenantId}";
        
        // Sliding window algorithm
        var requests = _cache.GetOrCreate(key, entry => {
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);
            return new List<DateTime>();
        });
        
        // Enforce limit...
    }
}
```

### 6.2 Error Handling

#### 6.2.1 Exception Handling Strategy
**Global Exception Middleware:**
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var response = ex switch
        {
            UnauthorizedAccessException => (401, "Unauthorized"),
            ValidationException => (400, ex.Message),
            NotFoundException => (404, "Resource not found"),
            _ => (500, "Internal server error")
        };
        
        context.Response.StatusCode = response.Item1;
        await context.Response.WriteAsJsonAsync(new { error = response.Item2 });
    }
}
```

#### 6.2.2 Error Responses
**Standard Error Format:**
```json
{
  "error": "Error message",
  "details": "Additional context",
  "timestamp": "2026-05-30T10:30:00Z",
  "traceId": "00-abc123-def456-00"
}
```

#### 6.2.3 Retry Policies
- **OpenAI API Calls**: Exponential backoff (3 retries, 1s, 2s, 4s delays)
- **Database Queries**: No automatic retry (fail fast, log error)
- **Transient Failures**: Circuit breaker pattern (future enhancement)

### 6.3 Logging and Monitoring

#### 6.3.1 Logging Framework
**Serilog Configuration:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/aiquery-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

#### 6.3.2 Log Levels
- **Debug**: Detailed execution flow, SQL queries (development only)
- **Information**: Request/response, query execution, tenant resolution
- **Warning**: Rate limit approaching, slow queries (>3s), schema cache miss
- **Error**: SQL errors, OpenAI failures, validation errors
- **Critical**: System-wide failures, database connection failures

#### 6.3.3 Structured Logging
```csharp
_logger.LogInformation(
    "Query executed: TenantId={TenantId}, Query={Query}, ExecutionTime={ExecutionTime}ms",
    tenantId, query, executionTime
);
```

#### 6.3.4 Monitoring Metrics (Future)
- **Request Rate**: Requests per second per tenant
- **Response Time**: P50, P95, P99 latency
- **Error Rate**: 4xx/5xx percentage
- **OpenAI Latency**: Time to SQL generation
- **Query Execution Time**: Database query duration
- **Token Usage**: Tokens consumed per tenant per day
- **Cache Hit Rate**: Schema cache effectiveness

**Recommended Tools:**
- **Application Insights**: Azure monitoring (integrated via Serilog sink)
- **Prometheus + Grafana**: Open-source metrics and dashboards
- **Seq**: Structured log viewer

### 6.4 Performance

#### 6.4.1 Caching Strategy
**Schema Caching:**
- **Cache Key**: `schema:{tenantId}`
- **TTL**: 1 hour (3600 seconds)
- **Invalidation**: Manual via `POST /api/query/clear-schema-cache`
- **Storage**: In-memory cache (IMemoryCache)

**Rate Limit Caching:**
- **Cache Key**: `ratelimit:{tenantId}:{window}`
- **TTL**: Sliding 1 minute or 1 hour window
- **Storage**: In-memory cache

#### 6.4.2 Database Optimization
**Connection Pooling:**
- Min Pool Size: 5
- Max Pool Size: 100
- Connection Timeout: 30 seconds

**Query Optimization:**
- **Row Limits**: Automatic TOP 100 enforcement
- **Query Timeout**: 30 seconds maximum
- **Prepared Statements**: Parameterized queries (future for template queries)

#### 6.4.3 Streaming Optimization
**IAsyncEnumerable Benefits:**
- Memory-efficient (yield return, no buffering)
- Immediate client feedback (progressive rendering)
- Cancellation support (client disconnect)

#### 6.4.4 Frontend Optimization
**Widget Loading:**
- **Loader Script**: 5KB minified (vanilla JS)
- **React Bundle**: Lazy loaded on first interaction
- **Code Splitting**: vendors.js separate (Chart.js, React)

**SDK Best Practices:**
- Debounce search inputs (300ms)
- Virtual scrolling for large result sets (future)
- Progressive image loading for charts

#### 6.4.5 PDF Generation Optimization
- **Memory**: Stream PDF directly to response (no disk write)
- **Limits**: Max 10,000 rows in PDF (configurable)
- **Compression**: Embedded images compressed

---

## 7. Detailed Design

### 7.1 QueryOrchestrationService

#### 7.1.1 Responsibilities
- Coordinate query execution pipeline (NL2SQL → Validation → Execution → Intelligence)
- Emit streaming events for real-time client feedback
- Handle errors at each pipeline stage
- Track token usage

#### 7.1.2 Interface
```csharp
public interface IQueryOrchestrationService
{
    IAsyncEnumerable<StreamEvent> ExecuteQueryStreamAsync(string query);
    Task<QueryResponse> ExecuteQueryAsync(string query);
}
```

#### 7.1.3 Implementation
```csharp
public class QueryOrchestrationService : IQueryOrchestrationService
{
    private readonly INLToSqlService _nlToSqlService;
    private readonly ISqlValidatorService _sqlValidatorService;
    private readonly IQueryExecutionService _queryExecutionService;
    private readonly IIntelligenceLayerService _intelligenceLayerService;
    private readonly ITokenUsageService _tokenUsageService;
    
    public async IAsyncEnumerable<StreamEvent> ExecuteQueryStreamAsync(string query)
    {
        yield return new StreamEvent { Type = "log", Message = "Processing query..." };
        
        // 1. NL to SQL
        var sqlResult = await _nlToSqlService.ConvertToSqlAsync(query);
        yield return new StreamEvent { Type = "sql_generated", Sql = sqlResult.Sql };
        
        // 2. Validate SQL
        var validationResult = await _sqlValidatorService.ValidateAsync(sqlResult.Sql);
        if (!validationResult.IsValid)
        {
            yield return new StreamEvent { Type = "error", Message = validationResult.ErrorMessage };
            yield break;
        }
        
        // 3. Execute SQL
        yield return new StreamEvent { Type = "execution_progress", Message = "Executing query..." };
        var executionResult = await _queryExecutionService.ExecuteQueryAsync(validationResult.SanitizedSql);
        
        // 4. Intelligence Layer
        var vizType = _intelligenceLayerService.DetermineVisualizationType(executionResult, query);
        
        // 5. Track tokens
        await _tokenUsageService.TrackUsageAsync(sqlResult.TokenUsage);
        
        yield return new StreamEvent
        {
            Type = "final_result",
            Data = executionResult,
            VisualizationType = vizType,
            TokenUsage = sqlResult.TokenUsage
        };
    }
}
```

### 7.2 NLToSqlService

#### 7.2.1 Responsibilities
- Integrate with Azure OpenAI for natural language understanding
- Build context-aware prompts with tenant schema
- Parse and clean AI-generated SQL
- Track token usage

#### 7.2.2 Interface
```csharp
public interface INLToSqlService
{
    Task<NLToSqlResult> ConvertToSqlAsync(string query);
}

public class NLToSqlResult
{
    public string Sql { get; set; }
    public TokenUsage TokenUsage { get; set; }
}
```

#### 7.2.3 Prompt Engineering
**System Prompt Template:**
```
You are a SQL expert. Convert the following natural language query to SQL.

Database Schema:
{schema}

Important Rules:
- Use only SELECT statements
- Return valid SQL for {databaseType}
- Use proper JOINs when referencing multiple tables
- Include appropriate WHERE clauses
- Return ONLY the SQL query, no explanations

User Query: {query}

SQL Query:
```

**Schema Injection:**
```csharp
private async Task<string> BuildPromptAsync(string query)
{
    var schema = await _schemaService.GetSchemaAsync();
    var schemaText = string.Join("\n", schema.Tables.Select(t =>
        $"Table: {t.Name}\nColumns: {string.Join(", ", t.Columns.Select(c => $"{c.Name} ({c.DataType})"))}"));
    
    return PromptTemplate
        .Replace("{schema}", schemaText)
        .Replace("{query}", query)
        .Replace("{databaseType}", _tenantContext.DatabaseType.ToString());
}
```

### 7.3 SqlValidatorService

#### 7.3.1 Responsibilities
- Block non-SELECT statements
- Detect SQL injection patterns
- Inject row limits
- Sanitize SQL for safe execution

#### 7.3.2 Interface
```csharp
public interface ISqlValidatorService
{
    Task<ValidationResult> ValidateAsync(string sql);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    public string SanitizedSql { get; set; }
}
```

#### 7.3.3 Validation Rules
```csharp
public async Task<ValidationResult> ValidateAsync(string sql)
{
    // 1. Remove comments
    var cleaned = RemoveComments(sql);
    
    // 2. Check forbidden keywords
    if (ForbiddenKeywords.Any(k => cleaned.Contains(k, StringComparison.OrdinalIgnoreCase)))
        return Invalid("Non-SELECT queries are not allowed");
    
    // 3. Check injection patterns
    if (Regex.IsMatch(cleaned, @"(--|/\*|\*/|;|UNION|@@|xp_)"))
        return Invalid("Potential SQL injection detected");
    
    // 4. Ensure single statement
    if (cleaned.Count(c => c == ';') > 1)
        return Invalid("Multiple statements not allowed");
    
    // 5. Inject row limit
    var sanitized = InjectRowLimit(cleaned);
    
    return Valid(sanitized);
}
```

### 7.4 QueryExecutionService

#### 7.4.1 Responsibilities
- Execute SQL against tenant database
- Handle database-specific connections (SQL Server, MySQL, PostgreSQL)
- Apply query timeouts
- Transform results to standard format

#### 7.4.2 Interface
```csharp
public interface IQueryExecutionService
{
    Task<QueryResult> ExecuteQueryAsync(string sql);
}

public class QueryResult
{
    public List<string> Columns { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; }
}
```

#### 7.4.3 Database Provider Abstraction
```csharp
public async Task<QueryResult> ExecuteQueryAsync(string sql)
{
    var connectionString = _tenantContext.Tenant.ConnectionString;
    var databaseType = _tenantContext.Tenant.DatabaseType;
    
    return databaseType switch
    {
        DatabaseType.SqlServer => await ExecuteSqlServerAsync(sql, connectionString),
        DatabaseType.MySql => await ExecuteMySqlAsync(sql, connectionString),
        DatabaseType.PostgreSql => await ExecutePostgreSqlAsync(sql, connectionString),
        DatabaseType.Sqlite => await ExecuteSqliteAsync(sql, connectionString),
        _ => throw new NotSupportedException($"Database type {databaseType} not supported")
    };
}

private async Task<QueryResult> ExecuteSqlServerAsync(string sql, string connectionString)
{
    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(sql, connection);
    command.CommandTimeout = 30; // seconds
    
    await connection.OpenAsync();
    using var reader = await command.ExecuteReaderAsync();
    
    var result = new QueryResult { Columns = new List<string>(), Rows = new List<Dictionary<string, object>>() };
    
    for (int i = 0; i < reader.FieldCount; i++)
        result.Columns.Add(reader.GetName(i));
    
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object>();
        foreach (var column in result.Columns)
            row[column] = reader[column] ?? DBNull.Value;
        result.Rows.Add(row);
    }
    
    return result;
}
```

### 7.5 IntelligenceLayerService

#### 7.5.1 Responsibilities
- Analyze query results to determine optimal visualization
- Detect numeric data for chart recommendations
- Identify report keywords for PDF generation

#### 7.5.2 Interface
```csharp
public interface IIntelligenceLayerService
{
    string DetermineVisualizationType(QueryResult result, string originalQuery);
}
```

#### 7.5.3 Decision Logic
```csharp
public string DetermineVisualizationType(QueryResult result, string originalQuery)
{
    // 1. Check for report keywords
    var reportKeywords = new[] { "report", "summary", "export", "document" };
    if (reportKeywords.Any(k => originalQuery.Contains(k, StringComparison.OrdinalIgnoreCase)))
        return "pdf";
    
    // 2. Check for numeric data suitable for charts
    if (result.Columns.Count >= 2 && result.Rows.Count > 1)
    {
        var numericColumns = result.Columns.Where(c =>
            result.Rows.Any(r => r[c] is int or long or decimal or double or float)).ToList();
        
        if (numericColumns.Count >= 1)
            return "chart";
    }
    
    // 3. Default to table
    return "table";
}
```

### 7.6 SchemaService

#### 7.6.1 Responsibilities
- Discover database schema via INFORMATION_SCHEMA
- Cache schema per tenant (1-hour TTL)
- Support multiple database types

#### 7.6.2 Interface
```csharp
public interface ISchemaService
{
    Task<SchemaResponse> GetSchemaAsync();
    Task ClearSchemaCacheAsync();
}
```

#### 7.6.3 Schema Discovery
```csharp
public async Task<SchemaResponse> GetSchemaAsync()
{
    var cacheKey = $"schema:{_tenantContext.TenantId}";
    
    if (_cache.TryGetValue(cacheKey, out SchemaResponse cached))
        return cached;
    
    var schema = await DiscoverSchemaAsync();
    _cache.Set(cacheKey, schema, TimeSpan.FromHours(1));
    
    return schema;
}

private async Task<SchemaResponse> DiscoverSchemaAsync()
{
    var sql = @"
        SELECT 
            t.TABLE_NAME,
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.IS_NULLABLE,
            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
        FROM INFORMATION_SCHEMA.TABLES t
        INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
        LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME AND c.TABLE_NAME = pk.TABLE_NAME
        WHERE t.TABLE_TYPE = 'BASE TABLE'
        ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION";
    
    var result = await _queryExecutionService.ExecuteQueryAsync(sql);
    return TransformToSchema(result);
}
```

### 7.7 TenantContext (Scoped Service)

#### 7.7.1 Responsibilities
- Hold current tenant information for the request
- Provide tenant-scoped data to services

#### 7.7.2 Interface
```csharp
public class TenantContext
{
    public string TenantId { get; set; }
    public Tenant Tenant { get; set; }
}
```

#### 7.7.3 Middleware Integration
```csharp
public class TenantResolutionMiddleware
{
    private readonly ITenantService _tenantService;
    private readonly TenantContext _tenantContext;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = context.Request.Headers["x-api-key"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API key required");
            return;
        }
        
        var tenant = await _tenantService.GetByApiKeyAsync(apiKey);
        
        if (tenant == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }
        
        _tenantContext.TenantId = tenant.TenantId.ToString();
        _tenantContext.Tenant = tenant;
        
        await _next(context);
    }
}
```

### 7.8 JavaScript SDK (AIQueryUI)

#### 7.8.1 Responsibilities
- Simplify API integration for frontend developers
- Handle streaming NDJSON parsing
- Render tables and charts automatically
- Manage API authentication

#### 7.8.2 Interface
```typescript
class AIQueryUI {
  constructor(config: {
    apiBaseUrl: string;
    apiKey: string;
    containerId: string;
    onLog?: (message: string) => void;
    onError?: (error: string) => void;
  });
  
  async executeQuery(query: string): Promise<void>;
  async executeQueryStreaming(query: string): Promise<void>;
  async generateReport(query: string): Promise<Blob>;
  async getSchema(): Promise<SchemaResponse>;
}
```

#### 7.8.3 Usage Example
```html
<script src="https://cdn.example.com/ai-query-sdk.js"></script>
<div id="query-container"></div>

<script>
  const aiQuery = new AIQueryUI({
    apiBaseUrl: 'https://api.example.com',
    apiKey: 'demo_api_key_12345',
    containerId: 'query-container'
  });
  
  aiQuery.executeQueryStreaming('Show top 10 customers by revenue');
</script>
```

### 7.9 Embeddable Widget

#### 7.9.1 Architecture
**Two-Phase Loading:**
1. **Loader Script** (5KB): Reads `data-*` attributes, creates Shadow DOM, lazy-loads React bundle
2. **React App Bundle**: Main widget UI loaded on demand

#### 7.9.2 Integration
```html
<!-- Single script tag integration -->
<script
  src="https://cdn.example.com/ai-widget.js"
  data-api-url="https://api.example.com"
  data-api-key="demo_api_key_12345"
  data-tenant-id="11111111-1111-1111-1111-111111111111"
  data-theme="light"
></script>
```

#### 7.9.3 Shadow DOM Isolation
```typescript
// Loader creates Shadow DOM
const shadowRoot = container.attachShadow({ mode: 'open' });

// Load styles into Shadow DOM (no leakage to host page)
const style = document.createElement('style');
style.textContent = widgetStyles;
shadowRoot.appendChild(style);

// Load React app
const app = document.createElement('div');
app.id = 'widget-root';
shadowRoot.appendChild(app);

// React renders inside Shadow DOM
ReactDOM.createRoot(app).render(<App config={config} />);
```

---

## 8. Appendix

### 8.1 Deployment Architecture

#### 8.1.1 Deployment Options

**Option 1: Azure App Service (Recommended)**
```
┌─────────────────────────────────────────────────────┐
│              Azure Front Door (CDN)                 │
│         (SSL, WAF, Load Balancing)                  │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│          Azure App Service (B1 or higher)           │
│          • .NET 8 Runtime                           │
│          • Auto-scale enabled                       │
│          • Health check: /api/health                │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│          Azure SQL Database (S1 or higher)          │
│          • Platform DB (AIQueryPlatform)            │
│          • Tenant DBs (per-tenant)                  │
│          • Geo-replication (optional)               │
└─────────────────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│          Azure OpenAI Service                       │
│          • GPT-4 deployment                         │
│          • Private endpoint (optional)              │
└─────────────────────────────────────────────────────┘
```

**Option 2: Docker Container (Self-Hosted)**
```bash
# Using docker-compose.yml
docker-compose up -d

# Services:
# - aiquery-api (port 7000)
# - sqlserver (port 1433)
```

#### 8.1.2 Environment Variables (Production)
```bash
# Azure OpenAI
OPENAI__ENDPOINT=https://your-instance.openai.azure.com/
OPENAI__APIKEY=<from-key-vault>
OPENAI__DEPLOYMENTNAME=gpt-4

# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION=<from-key-vault>

# Logging
SERILOG__MINIMUMLEVEL__DEFAULT=Information

# Rate Limiting
RATELIMITING__PERMINUTELIMIT=60
RATELIMITING__PERHOURLIMIT=1000
```

### 8.2 Sequence Diagrams

#### 8.2.1 Streaming Query Execution

```
Client          Middleware         Controller         Orchestrator         NLToSql         Validator         Executor         Intelligence
  │                  │                  │                  │                  │                  │                  │                  │
  ├─POST /execute-stream────────────────►│                  │                  │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  ├─Resolve Tenant───►│                  │                  │                  │                  │                  │
  │                  ◄─────────────────  │                  │                  │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  ├─Check Rate Limit─►│                  │                  │                  │                  │                  │
  │                  ◄─────────────────  │                  │                  │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  │                  ├─ExecuteStreamAsync────────────────►│                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │◄─────────────────────────────────────────Yield: "Processing query..."    │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  │                  │                  ├─ConvertToSqlAsync────────────────►│                  │                  │
  │                  │                  │                  │                  ├─GetSchema (cached)────────────────►│                  │
  │                  │                  │                  │                  ├─Call OpenAI API─►│                  │                  │
  │                  │                  │                  │                  ◄──────────────── │                  │                  │
  │                  │                  │                  ◄──────────────────────────────────  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │◄─────────────────────────────────────────Yield: "sql_generated"          │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  │                  │                  ├─ValidateAsync─────────────────────────────────────────►│                  │
  │                  │                  │                  ◄───────────────────────────────────────────────────────  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │◄─────────────────────────────────────────Yield: "execution_progress"     │                  │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  │                  │                  ├─ExecuteQueryAsync────────────────────────────────────────────────────────►│
  │                  │                  │                  ◄──────────────────────────────────────────────────────────────────────────  │
  │                  │                  │                  │                  │                  │                  │                  │
  │                  │                  │                  ├─DetermineVisualizationType──────────────────────────────────────────────────────────────────►│
  │                  │                  │                  ◄──────────────────────────────────────────────────────────────────────────────────────────────  │
  │                  │                  │                  │                  │                  │                  │                  │
  │◄─────────────────────────────────────────Yield: "final_result" (data + viz type)           │                  │                  │
  │                  │                  │                  │                  │                  │                  │                  │
```

### 8.3 State Diagrams

#### 8.3.1 Query Execution States

```
┌─────────┐
│ PENDING │ (Initial state)
└────┬────┘
     │
     ▼
┌─────────────┐
│ CONVERTING  │ (NL to SQL via OpenAI)
└────┬────────┘
     │
     ▼
┌─────────────┐
│ VALIDATING  │ (Security checks)
└────┬────────┘
     │
     ├──[Invalid]──► ┌────────┐
     │              │ FAILED │
     │              └────────┘
     │
     ▼
┌─────────────┐
│ EXECUTING   │ (Running SQL query)
└────┬────────┘
     │
     ├──[Error]────► ┌────────┐
     │              │ FAILED │
     │              └────────┘
     │
     ▼
┌─────────────┐
│ ANALYZING   │ (Intelligence Layer)
└────┬────────┘
     │
     ▼
┌─────────────┐
│ COMPLETED   │ (Success with results)
└─────────────┘
```

### 8.4 Technology Stack Summary

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| **Backend Runtime** | .NET | 8.0 | API server runtime |
| **Web Framework** | ASP.NET Core | 8.0 | HTTP endpoints, middleware |
| **AI/LLM** | Azure OpenAI | GPT-4 | Natural language to SQL |
| **Database (Platform)** | SQL Server | 2019+ | Tenant management |
| **Database (Tenant)** | SQL Server / MySQL / PostgreSQL | Various | Business data |
| **Caching** | IMemoryCache | Built-in | Schema and rate limits |
| **Logging** | Serilog | 3.x | Structured logging |
| **PDF Generation** | iTextSharp | 3.x (LGPLv2) | Report generation |
| **Frontend Framework** | React | 18.x | Widget UI |
| **Build Tool** | Webpack | 5.x | Bundle widget |
| **Type Safety** | TypeScript | 5.x | SDK and widget |
| **Visualization** | Chart.js | 4.x | Client-side charts |
| **Containerization** | Docker | 20.x | Deployment |

### 8.5 Glossary

**Tenant**: An independent customer of the platform with isolated database and API key

**NL2SQL**: Process of converting natural language queries to SQL using AI

**NDJSON**: Newline Delimited JSON - streaming format where each line is a valid JSON object

**IAsyncEnumerable**: .NET feature for async streaming with yield return pattern

**Shadow DOM**: Web standard for component isolation (CSS/JS scoped to component)

**Lazy Loading**: Loading code/resources on demand rather than upfront

**Rate Limiting**: Restricting number of API requests per time window

**Row Limit**: Maximum number of rows returned from a query (100 default)

**Schema Discovery**: Runtime inspection of database structure via INFORMATION_SCHEMA

**Streaming Response**: Progressive data delivery (results sent as they're generated)

**Middleware Pipeline**: Request processing chain (auth → rate limit → routing → controller)

**Scoped Service**: Dependency injection lifetime - one instance per request

**Circuit Breaker**: Fault tolerance pattern that prevents cascading failures

**Token Usage**: OpenAI API consumption measured in tokens (input + output)

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-30 | Development Team | Initial comprehensive TSD |

---

**END OF DOCUMENT**
