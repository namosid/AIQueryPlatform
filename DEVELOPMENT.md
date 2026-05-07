# Development Guide

## Project Structure

```
AIQueryPlatform/
├── src/
│   └── AIQueryPlatform.Api/
│       ├── Controllers/           # API controllers
│       ├── Middleware/            # Custom middleware
│       ├── Models/                # Domain models and DTOs
│       ├── Services/              # Business logic services
│       │   ├── Interfaces/        # Service interfaces
│       │   └── *.cs               # Service implementations
│       ├── Program.cs             # Application entry point
│       └── appsettings.json       # Configuration
├── frontend/
│   ├── aiquery-sdk.js            # Frontend SDK
│   └── index.html                # Demo UI
├── database/
│   └── setup.sql                 # Database setup script
└── README.md
```

## Architecture Overview

### Request Pipeline

```
HTTP Request
    ↓
ExceptionHandlingMiddleware (Global error handling)
    ↓
TenantResolutionMiddleware (Resolve tenant from API key)
    ↓
RateLimitingMiddleware (Enforce rate limits)
    ↓
Controller (QueryController / TenantController)
    ↓
QueryOrchestrationService (Orchestrates the pipeline)
    ↓
├─→ SchemaService (Get database schema)
├─→ NLToSqlService (Convert NL to SQL using AI)
├─→ SqlValidatorService (Validate and secure SQL)
├─→ QueryExecutionService (Execute SQL)
├─→ IntelligenceLayerService (Determine visualization)
└─→ ReportingService (Generate PDF if needed)
    ↓
HTTP Response (JSON / NDJSON / PDF)
```

### Multi-Tenant Architecture

Each tenant has:
- **Unique API Key**: Used for authentication
- **Isolated Database**: Separate connection string
- **Custom Branding**: Logo and theme color
- **Rate Limits**: Per-tenant request throttling

The `TenantContext` is a scoped service that holds the current tenant for the request.

## Key Components

### 1. Middleware

**TenantResolutionMiddleware**
- Resolves tenant from `x-api-key` or `x-tenant-id` header
- Injects tenant into `TenantContext`
- Returns 401 if authentication fails

**RateLimitingMiddleware**
- Enforces per-tenant rate limits
- Uses in-memory cache for tracking
- Returns 429 when limit exceeded

**ExceptionHandlingMiddleware**
- Catches all unhandled exceptions
- Returns proper HTTP status codes
- Logs errors with Serilog

### 2. Services

**TenantService**
- Manages tenant data
- Currently uses in-memory store (replace with DB in production)
- Caches tenant lookups

**NLToSqlService**
- Integrates with Azure OpenAI / OpenAI
- Builds context-aware prompts with schema
- Returns clean SQL queries

**SqlValidatorService**
- Blocks dangerous SQL keywords
- Enforces SELECT-only execution
- Detects SQL injection patterns
- Adds row limits automatically

**QueryExecutionService**
- Executes SQL against tenant database
- Returns normalized results
- Handles errors gracefully

**SchemaService**
- Loads database schema dynamically
- Caches schema per tenant (1 hour)
- Supports schema invalidation

**IntelligenceLayerService**
- Analyzes query and results
- Determines best visualization (Table/Chart/PDF)
- Converts data to chart format

**ReportingService**
- Generates PDF reports
- Includes tenant branding
- Supports tables and chart summaries

**QueryOrchestrationService**
- Orchestrates entire pipeline
- Supports streaming (NDJSON)
- Provides comprehensive logging

### 3. Controllers

**QueryController**
- `POST /api/query/execute-stream` - Streaming query execution
- `POST /api/query/execute` - Standard query execution
- `POST /api/query/generate-report` - PDF report generation
- `POST /api/query/to-chart` - Convert result to chart data

**TenantController**
- `GET /api/tenant/current` - Get current tenant info
- `POST /api/tenant/invalidate-cache` - Clear schema cache
- `GET /api/tenant/health` - Health check

## Adding New Features

### Add a New Service

1. Create interface in `Services/Interfaces/`
```csharp
public interface IMyService
{
    Task<Result> DoSomethingAsync(string input);
}
```

2. Implement service in `Services/`
```csharp
public class MyService : IMyService
{
    public async Task<Result> DoSomethingAsync(string input)
    {
        // Implementation
    }
}
```

3. Register in `Program.cs`
```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

### Add a New Endpoint

1. Add method to controller
```csharp
[HttpPost("my-endpoint")]
public async Task<ActionResult> MyEndpoint([FromBody] Request request)
{
    // Implementation
    return Ok(result);
}
```

### Add a New Middleware

1. Create middleware class
```csharp
public class MyMiddleware
{
    private readonly RequestDelegate _next;
    
    public MyMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Before request
        await _next(context);
        // After request
    }
}
```

2. Register in `Program.cs`
```csharp
app.UseMiddleware<MyMiddleware>();
```

## Testing

### Manual Testing with Swagger
1. Run the application
2. Navigate to `https://localhost:7001/swagger`
3. Click "Authorize" and enter API key: `demo_api_key_12345`
4. Test endpoints

### Testing with Frontend
1. Open `frontend/index.html` in browser
2. Verify API URL and API key
3. Try example queries

### Testing with cURL
```bash
# Execute query
curl -X POST https://localhost:7001/api/query/execute \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{"query":"Show me top 5 customers"}'

# Streaming query
curl -X POST https://localhost:7001/api/query/execute-stream \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{"query":"Show me top 5 customers"}' \
  --no-buffer
```

## Best Practices

### Security
- Always validate API keys
- Enforce SELECT-only queries
- Sanitize all user inputs
- Use parameterized queries
- Implement rate limiting
- Log security events

### Performance
- Cache database schemas
- Use async/await everywhere
- Implement connection pooling
- Add response caching for static data
- Optimize SQL queries with TOP/LIMIT

### Code Quality
- Follow SOLID principles
- Use dependency injection
- Write meaningful logs
- Handle exceptions gracefully
- Use DTOs for API contracts
- Document complex logic

### Logging
Log important events:
- User queries (sanitized)
- Generated SQL
- Execution times
- Errors and warnings
- Security events

```csharp
_logger.LogInformation("Processing query for tenant {TenantId}", tenantId);
_logger.LogWarning("Rate limit exceeded for tenant {TenantId}", tenantId);
_logger.LogError(ex, "Query execution failed");
```

## Common Customizations

### Change AI Model
Update `appsettings.json`:
```json
{
  "OpenAI": {
    "DeploymentName": "gpt-4-turbo",
    "MaxTokens": 2000,
    "Temperature": 0.1
  }
}
```

### Adjust Rate Limits
Update `appsettings.json`:
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 100,
    "RequestsPerHour": 5000
  }
}
```

### Change Row Limit
Update `appsettings.json`:
```json
{
  "QueryExecution": {
    "MaxRowLimit": 200
  }
}
```

### Add Custom Validation
Modify `SqlValidatorService.cs`:
```csharp
private static readonly HashSet<string> DangerousKeywords = new()
{
    "INSERT", "UPDATE", "DELETE", "DROP",
    "MY_CUSTOM_KEYWORD"
};
```

## Troubleshooting

### Enable Detailed Logging
Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### Check Logs
Logs are in `logs/aiquery-YYYYMMDD.log`

### Common Issues
- **Can't connect to database**: Check connection string
- **AI not responding**: Verify OpenAI endpoint and key
- **Rate limit too aggressive**: Adjust in configuration
- **SQL validation too strict**: Modify SqlValidatorService

## Contributing

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Update documentation
5. Submit pull request

## Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Serilog Documentation](https://serilog.net/)
