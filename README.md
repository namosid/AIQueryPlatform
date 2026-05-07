# AI-Powered Multi-Tenant Query & Reporting Platform

A production-grade .NET 8 Web API platform that enables natural language to SQL conversion with multi-tenant isolation, streaming responses, and intelligent visualization recommendations.

## Features

- ✅ **Multi-Tenant Architecture** - Complete tenant isolation with API key authentication
- ✅ **AI-Powered Query Engine** - Natural language to SQL using Azure OpenAI/OpenAI
- ✅ **Security Hardening** - SELECT-only enforcement, SQL injection prevention, rate limiting
- ✅ **Streaming Responses** - Real-time NDJSON streaming with IAsyncEnumerable
- ✅ **Intelligence Layer** - Automatic UI decision (table/chart/PDF) based on data
- ✅ **PDF Reporting** - Branded reports with tables and charts
- ✅ **Frontend SDK** - JavaScript SDK for easy integration
- ✅ **Observability** - Comprehensive logging with Serilog
- ✅ **Clean Architecture** - Proper separation of concerns with DI

## Architecture

```
┌─────────────────┐
│  Frontend SDK   │
│   (JavaScript)  │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│         API Gateway Layer               │
│  ┌──────────────────────────────────┐  │
│  │  Tenant Resolution Middleware    │  │
│  │  API Key Auth Middleware         │  │
│  │  Rate Limiting Middleware        │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│           Controllers Layer             │
│  • QueryController (streaming)          │
│  • TenantController                     │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│          Services Layer                 │
│  • NLToSqlService (AI)                  │
│  • SqlValidatorService                  │
│  • QueryExecutionService                │
│  • IntelligenceLayerService             │
│  • ReportingService                     │
│  • SchemaService                        │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│         Data Access Layer               │
│  • Tenant-specific connections          │
│  • Schema caching                       │
└─────────────────────────────────────────┘
```

## Quick Start

### Prerequisites

- .NET 8 SDK
- SQL Server (or any supported database)
- Azure OpenAI or OpenAI API key

### Setup

1. **Clone and configure**
   ```bash
   cd AIQueryPlatform
   ```

2. **Update appsettings.json**
   - Set your OpenAI endpoint and API key
   - Configure database connection string

3. **Run the application**
   ```bash
   dotnet run --project src/AIQueryPlatform.Api
   ```

4. **Test with frontend SDK**
   - Open `frontend/index.html` in a browser
   - Enter your tenant API key
   - Start querying with natural language

## API Endpoints

### Query Execution (Streaming)
```http
POST /api/query/execute-stream
Headers:
  x-api-key: your-tenant-api-key
  Content-Type: application/json

Body:
{
  "query": "Show me top 10 customers by revenue"
}

Response: NDJSON stream
{"type":"log","message":"Processing query..."}
{"type":"sql_generated","sql":"SELECT TOP 10..."}
{"type":"execution_progress","message":"Executing query..."}
{"type":"final_result","data":{...},"visualizationType":"table"}
```

### Non-Streaming Query
```http
POST /api/query/execute
Headers:
  x-api-key: your-tenant-api-key

Body:
{
  "query": "What are the total sales this month?"
}
```

### Generate Report
```http
POST /api/query/generate-report
Headers:
  x-api-key: your-tenant-api-key

Body:
{
  "query": "Sales summary report"
}

Response: PDF file
```

## Multi-Tenant Setup

### Register a New Tenant

Update the tenant data in your database:

```sql
INSERT INTO Tenants (TenantId, Name, ApiKey, ConnectionString, LogoUrl, ThemeColor)
VALUES (
  NEWID(),
  'Acme Corp',
  'acme_prod_key_12345',
  'Server=localhost;Database=AcmeDB;...',
  'https://example.com/logo.png',
  '#FF6600'
);
```

### Tenant Isolation

- Each request must include `x-api-key` header
- Middleware resolves tenant and injects TenantContext
- All database queries use tenant-specific connection strings
- Complete data isolation between tenants

## Security Features

### SQL Injection Prevention
- Keyword blacklist (INSERT, UPDATE, DELETE, DROP, ALTER, etc.)
- Parameterized queries enforcement
- SELECT-only execution

### Rate Limiting
- Per-tenant request throttling
- Configurable limits (requests per minute/hour)

### Query Safety
- Automatic TOP/LIMIT injection (max 100 rows)
- Query timeout enforcement (30 seconds default)
- Table allowlist per tenant

## Frontend SDK

### Installation

```html
<script src="aiquery-sdk.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

### Usage

```javascript
// Initialize SDK
const sdk = new AIQueryUI({
  apiUrl: 'https://localhost:5001',
  apiKey: 'your-tenant-api-key',
  resultContainerId: 'result'
});

// Execute query with streaming
await sdk.sendQuery('Show me top products by sales');

// Result will be automatically rendered as table or chart
```

## Intelligence Layer

The system automatically decides the best visualization:

- **TABLE**: Default for most queries
- **CHART**: When result has 2 columns with numeric data
- **PDF**: When query contains report keywords (report, summary, export)

## Streaming Protocol

NDJSON format with event types:

```json
{"type":"log","message":"Processing query...","timestamp":"2026-05-05T10:30:00Z"}
{"type":"sql_generated","sql":"SELECT TOP 10 * FROM Customers","timestamp":"2026-05-05T10:30:01Z"}
{"type":"execution_progress","message":"Executing query...","timestamp":"2026-05-05T10:30:02Z"}
{"type":"final_result","data":{"columns":[...],"rows":[...]},"visualizationType":"table","timestamp":"2026-05-05T10:30:03Z"}
```

## Configuration

### OpenAI Settings
```json
{
  "OpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "ApiKey": "sk-...",
    "DeploymentName": "gpt-4",
    "MaxTokens": 1000,
    "Temperature": 0.0
  }
}
```

### Rate Limiting
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 60,
    "RequestsPerHour": 1000
  }
}
```

### Query Execution
```json
{
  "QueryExecution": {
    "MaxRowLimit": 100,
    "QueryTimeoutSeconds": 30
  }
}
```

## Logging

All operations are logged with Serilog:
- User queries (sanitized)
- Generated SQL
- Execution time
- Errors and exceptions
- Tenant context

Logs are written to:
- Console (development)
- File: `logs/aiquery-.log` (production)

## Error Handling

Comprehensive error handling with proper HTTP status codes:
- 400: Bad Request (invalid query)
- 401: Unauthorized (missing/invalid API key)
- 403: Forbidden (dangerous SQL detected)
- 429: Too Many Requests (rate limit exceeded)
- 500: Internal Server Error

## Production Checklist

- [ ] Configure production OpenAI endpoint
- [ ] Set up proper database connections for each tenant
- [ ] Configure HTTPS with valid certificates
- [ ] Set up proper logging infrastructure
- [ ] Implement database schema caching
- [ ] Configure CORS for your frontend domains
- [ ] Set up monitoring and alerts
- [ ] Review and adjust rate limits
- [ ] Implement tenant onboarding workflow
- [ ] Set up backup and disaster recovery

## License

MIT License - See LICENSE file for details

## Sample Queries

See [SAMPLE_QUERIES.md](SAMPLE_QUERIES.md) for 40+ advanced query examples including:
- Sales & Revenue Intelligence
- Operational Insights
- Customer Behavior Analytics
- Financial & Profitability Analysis
- Predictive & Anomaly Detection
- Cross-Functional Business Questions
- Compliance & Risk Analytics
- Strategic Planning Queries

These demonstrate complex analytical capabilities that typically require SQL expertise or custom reports in traditional software.

## Support

For issues and questions, please open an issue on GitHub.
