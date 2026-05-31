# Technical Specification Document - Continuation
## Sections 8-17

---

# 8. API Contracts

## 8.1 Query Execution API

### POST /api/query/execute-stream

**Request Example**
```json
{
  "query": "Show me top 10 customers by revenue this year",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "context": "Sales data analysis for Q1 2026",
  "userRole": "Sales Manager",
  "conversationId": "conv-123-456-789"
}
```

**Response Example (NDJSON Stream)**
```json
{"type":"log","message":"Processing query...","timestamp":"2026-05-30T10:30:00Z"}
{"type":"sql_generated","sql":"SELECT TOP 10 c.Name, SUM(o.TotalAmount) as Revenue FROM Customers c INNER JOIN Orders o ON c.CustomerId = o.CustomerId WHERE YEAR(o.OrderDate) = 2026 GROUP BY c.Name ORDER BY Revenue DESC","timestamp":"2026-05-30T10:30:02Z"}
{"type":"execution_progress","message":"Executing query...","timestamp":"2026-05-30T10:30:03Z"}
{"type":"final_result","data":{"columns":["Name","Revenue"],"rows":[{"Name":"Acme Corp","Revenue":1500000},{"Name":"TechStart Inc","Revenue":1200000}],"visualizationType":"chart"},"tokenUsage":{"promptTokens":450,"completionTokens":85,"totalTokens":535},"timestamp":"2026-05-30T10:30:05Z"}
```

**Error Example**
```json
{
  "type": "error",
  "message": "SQL validation failed: Non-SELECT queries are not allowed",
  "timestamp": "2026-05-30T10:30:05Z"
}
```

---

### POST /api/query/execute

**Request Example**
```json
{
  "query": "What is the average order value in the last 30 days?"
}
```

**Response Example (Success)**
```json
{
  "success": true,
  "data": {
    "columns": ["AverageOrderValue"],
    "rows": [
      {"AverageOrderValue": 2534.67}
    ],
    "visualizationType": "table"
  },
  "sql": "SELECT AVG(TotalAmount) as AverageOrderValue FROM Orders WHERE OrderDate >= DATEADD(day, -30, GETDATE())",
  "executionTime": 245,
  "timestamp": "2026-05-30T10:30:00Z",
  "tokenUsage": {
    "promptTokens": 380,
    "completionTokens": 42,
    "totalTokens": 422
  }
}
```

**Error Example**
```json
{
  "success": false,
  "error": "Rate limit exceeded",
  "details": "You have exceeded your quota of 60 requests per minute",
  "timestamp": "2026-05-30T10:30:00Z",
  "traceId": "00-abc123def456-789ghi-00"
}
```

---

### POST /api/query/generate-report

**Request Example**
```json
{
  "query": "Generate quarterly sales report by product category"
}
```

**Response Example**
- **Content-Type**: `application/pdf`
- **Content-Disposition**: `attachment; filename="report-2026-05-30.pdf"`
- **Body**: PDF binary data

**Error Example**
```json
{
  "errorCode": "REPORT_GEN_001",
  "message": "PDF generation failed: result set too large",
  "details": "Maximum 10,000 rows allowed in PDF reports",
  "timestamp": "2026-05-30T10:30:00Z"
}
```

---

## 8.2 Schema API

### GET /api/query/schema

**Request**: No body, `x-api-key` header required

**Response Example**
```json
{
  "tables": [
    {
      "name": "Customers",
      "columns": [
        {
          "name": "CustomerId",
          "dataType": "int",
          "isNullable": false,
          "isPrimaryKey": true
        },
        {
          "name": "Name",
          "dataType": "nvarchar",
          "isNullable": false,
          "isPrimaryKey": false
        },
        {
          "name": "Email",
          "dataType": "nvarchar",
          "isNullable": true,
          "isPrimaryKey": false
        }
      ]
    },
    {
      "name": "Orders",
      "columns": [
        {
          "name": "OrderId",
          "dataType": "int",
          "isNullable": false,
          "isPrimaryKey": true
        },
        {
          "name": "CustomerId",
          "dataType": "int",
          "isNullable": false,
          "isPrimaryKey": false
        },
        {
          "name": "TotalAmount",
          "dataType": "decimal",
          "isNullable": false,
          "isPrimaryKey": false
        }
      ]
    }
  ],
  "cachedAt": "2026-05-30T09:30:00Z"
}
```

---

## 8.3 Tenant Management API

### POST /api/tenant

**Request Example**
```json
{
  "name": "Acme Corporation",
  "apiKey": "acme_prod_key_xyz789",
  "connectionString": "Server=localhost;Database=AcmeDB;Trusted_Connection=true;",
  "databaseType": 0,
  "logoUrl": "https://acme.com/logo.png",
  "themeColor": "#FF6600",
  "isActive": true
}
```

**Response Example**
```json
{
  "tenantId": "22222222-2222-2222-2222-222222222222",
  "name": "Acme Corporation",
  "apiKey": "acme_prod_key_xyz789",
  "connectionString": "Server=localhost;Database=AcmeDB;Trusted_Connection=true;",
  "databaseType": 0,
  "logoUrl": "https://acme.com/logo.png",
  "themeColor": "#FF6600",
  "isActive": true,
  "createdAt": "2026-05-30T10:30:00Z",
  "updatedAt": null
}
```

**Error Example**
```json
{
  "errorCode": "TENANT_001",
  "message": "Validation failed",
  "details": "API key 'acme_prod_key_xyz789' already exists",
  "timestamp": "2026-05-30T10:30:00Z"
}
```

---

## 8.4 Token Usage API

### GET /api/tokenusage?tenantId={tenantId}&startDate={date}&endDate={date}

**Request**: Query parameters
- `tenantId` (required): Tenant GUID
- `startDate` (optional): ISO 8601 date
- `endDate` (optional): ISO 8601 date

**Response Example**
```json
{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "startDate": "2026-05-01T00:00:00Z",
  "endDate": "2026-05-30T23:59:59Z",
  "totalTokens": 1250000,
  "totalCost": 37.50,
  "usage": [
    {
      "date": "2026-05-30",
      "promptTokens": 45000,
      "completionTokens": 8500,
      "totalTokens": 53500,
      "estimatedCost": 1.605,
      "queryCount": 120
    }
  ]
}
```

---

# 9. Sequence Diagrams

## 9.1 Login/Authentication Flow (API Key-Based)

```
Client                  API Gateway              TenantService            Platform DB
  │                          │                         │                      │
  ├─1. Request with x-api-key─►                        │                      │
  │                          │                         │                      │
  │                          ├─2. Extract API key────►│                      │
  │                          │                         │                      │
  │                          │                         ├─3. Query tenant────►│
  │                          │                         │   SELECT * FROM       │
  │                          │                         │   Tenants WHERE      │
  │                          │                         │   ApiKey = @key      │
  │                          │                         │                      │
  │                          │                         ◄─4. Tenant data──────┤
  │                          │                         │                      │
  │                          ◄─5. TenantContext────────┤                      │
  │                          │                         │                      │
  ◄─6. 200 OK / 401 Unauthorized                       │                      │
  │                          │                         │                      │
```

## 9.2 CRUD Operations - Create Conversation

```
Client          Controller      ConversationService    Platform DB       TokenUsageService
  │                  │                  │                  │                  │
  ├─1. POST /conversations────►│                  │                  │
  │                  │                  │                  │                  │
  │                  ├─2. CreateAsync──────────────►│                  │
  │                  │                  │                  │                  │
  │                  │                  ├─3. INSERT Conversation────────────►│
  │                  │                  │   (Title, Query, SQL, Result)      │
  │                  │                  │                  │                  │
  │                  │                  ◄─4. ConversationId─────────────────┤
  │                  │                  │                  │                  │
  │                  │                  ├─5. TrackTokenUsage────────────────────────►│
  │                  │                  │   (ConversationId, Tokens)                 │
  │                  │                  │                  │                  │
  │                  │                  │                  │                  ├─6. INSERT TokenUsage─►│
  │                  │                  │                  │                  │                        │
  │                  ◄─7. Conversation created───────────┤                  │
  │                  │                  │                  │                  │
  ◄─8. 201 Created───┤                  │                  │                  │
  │   (Conversation object)             │                  │                  │
```

## 9.3 External Integrations - OpenAI API Call

```
QueryOrchestrator    NLToSqlService   LLMServicePipe    Azure OpenAI    SchemaService
  │                        │                  │                │                │
  ├─1. ConvertToSqlAsync───►│                  │                │                │
  │                        │                  │                │                │
  │                        ├─2. GetSchemaAsync─────────────────────────────────►│
  │                        │                  │                │                │
  │                        ◄─3. Schema JSON (cached)───────────────────────────┤
  │                        │                  │                │                │
  │                        ├─4. BuildPrompt──►│                │                │
  │                        │   (Query + Schema)                │                │
  │                        │                  │                │                │
  │                        │                  ├─5. POST /chat/completions────►│
  │                        │                  │   {                            │
  │                        │                  │     "model": "gpt-4",          │
  │                        │                  │     "messages": [...],         │
  │                        │                  │     "temperature": 0           │
  │                        │                  │   }                            │
  │                        │                  │                │                │
  │                        │                  ◄─6. Response────────────────────┤
  │                        │                  │   {                            │
  │                        │                  │     "choices": [{              │
  │                        │                  │       "message": {             │
  │                        │                  │         "content": "SELECT..."  │
  │                        │                  │       }                        │
  │                        │                  │     }],                        │
  │                        │                  │     "usage": {...}             │
  │                        │                  │   }                            │
  │                        │                  │                │                │
  │                        ◄─7. SQL + Tokens──┤                │                │
  │                        │                  │                │                │
  ◄─8. NLToSqlResult───────┤                  │                │                │
  │   (SQL, TokenUsage)     │                  │                │                │
```

## 9.4 Error Handling Flow

```
Client          ExceptionMiddleware    Controller    Service      Database
  │                    │                  │              │              │
  ├─1. Request─────────►│                  │              │              │
  │                    │                  │              │              │
  │                    ├─2. Invoke next───────────────►│              │
  │                    │                  │              │              │
  │                    │                  ├─3. Process──────────────►│              │
  │                    │                  │              │              │
  │                    │                  │              │              ├─4. SQLException─►
  │                    │                  │              │              │   (Connection timeout)
  │                    │                  │              │              │
  │                    │                  │              ◄─5. Exception─┤
  │                    │                  │              │
  │                    │                  ◄─6. Exception─┤
  │                    │                  │
  │                    ◄─7. Exception caught──────────┤
  │                    │
  │                    ├─8. Log error (Serilog)
  │                    │   {
  │                    │     "Level": "Error",
  │                    │     "Message": "Database timeout",
  │                    │     "Exception": {...},
  │                    │     "TenantId": "..."
  │                    │   }
  │                    │
  │                    ├─9. Map to HTTP response
  │                    │   Status: 503 Service Unavailable
  │                    │   Body: { "error": "Database unavailable" }
  │                    │
  ◄─10. Error Response─┤
  │   {
  │     "error": "Database unavailable",
  │     "traceId": "00-abc-def-00",
  │     "timestamp": "2026-05-30T10:30:00Z"
  │   }
```

---

# 10. Exception Handling Strategy

## 10.1 Global Exception Middleware

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteErrorResponse(context, 400, "VAL001", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteErrorResponse(context, 401, "AUTH001", "Unauthorized");
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded");
            await WriteErrorResponse(context, 429, "RATE001", "Rate limit exceeded");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error occurred");
            await WriteErrorResponse(context, 503, "DB001", "Database unavailable");
        }
        catch (OpenAIException ex)
        {
            _logger.LogError(ex, "OpenAI API error");
            await WriteErrorResponse(context, 502, "AI001", "AI service unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception");
            await WriteErrorResponse(context, 500, "SYS001", "Internal server error");
        }
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            errorCode,
            message,
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}
```

## 10.2 Exception Types

### Validation Exceptions
```csharp
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public Dictionary<string, string[]> Errors { get; set; }
}

// Usage
throw new ValidationException("Query cannot be empty")
{
    Errors = new Dictionary<string, string[]>
    {
        { "query", new[] { "Query is required", "Query must be between 3 and 500 characters" } }
    }
};
```

### Business Exceptions
```csharp
public class TenantNotFoundException : Exception
{
    public TenantNotFoundException(string tenantId) 
        : base($"Tenant with ID '{tenantId}' not found") { }
}

public class SqlValidationException : Exception
{
    public SqlValidationException(string message) : base(message) { }
    public string InvalidSql { get; set; }
    public string Reason { get; set; }
}
```

### Infrastructure Exceptions
```csharp
public class OpenAIException : Exception
{
    public OpenAIException(string message, Exception inner) : base(message, inner) { }
    public int? StatusCode { get; set; }
    public string ResponseBody { get; set; }
}

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string tenantId, int limit) 
        : base($"Rate limit of {limit} requests exceeded for tenant {tenantId}") { }
    public int Limit { get; set; }
    public int Current { get; set; }
}
```

## 10.3 Error Response Format

```typescript
interface ErrorResponse {
  errorCode: string;        // Machine-readable error code
  message: string;          // Human-readable error message
  details?: string;         // Additional context (optional)
  timestamp: string;        // ISO 8601 UTC timestamp
  traceId: string;          // Distributed tracing correlation ID
  errors?: Record<string, string[]>;  // Field-level validation errors
}
```

---

# 11. Security Specification

## 11.1 Authentication Flow

```
┌─────────┐                           ┌─────────────────┐
│ Client  │                           │  API Gateway    │
└────┬────┘                           └────────┬────────┘
     │                                         │
     ├─1. Include x-api-key in header─────────►
     │                                         │
     │                                         ├─2. Extract API key
     │                                         │
     │                                         ├─3. Query Tenants table
     │                                         │   WHERE ApiKey = @key
     │                                         │
     │                                         ├─4. Validate tenant exists
     │                                         │   AND IsActive = 1
     │                                         │
     │                                         ├─5. Create TenantContext
     │                                         │   (scoped per request)
     │                                         │
     ◄─6. Request proceeds with context───────┤
     │                                         │
     │   OR                                    │
     │                                         │
     ◄─7. 401 Unauthorized (invalid key)──────┤
```

## 11.2 Authorization Flow

**Role-Based Access (Future Enhancement)**
```csharp
[Authorize(Roles = "Admin")]
public class TenantController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] TenantRequest request)
    {
        // Only admins can create tenants
    }
}

[Authorize(Roles = "User,Admin")]
public class QueryController : ControllerBase
{
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
    {
        // Users and admins can execute queries
    }
}
```

## 11.3 Token Lifecycle

**Current Implementation: Static API Keys**
- **Generation**: Manual via admin interface or SQL INSERT
- **Storage**: Platform database (Tenants table)
- **Validation**: Every request via TenantResolutionMiddleware
- **Expiration**: None (manual rotation required)
- **Revocation**: Set `IsActive = 0` in Tenants table

**Future: JWT Tokens**
```typescript
interface JWTPayload {
  sub: string;              // Tenant ID
  iss: string;              // Issuer (AIQueryPlatform)
  aud: string;              // Audience (api.aiquery.com)
  exp: number;              // Expiration timestamp
  iat: number;              // Issued at timestamp
  roles: string[];          // User roles
}

// Token lifecycle
// 1. Login → Issue JWT (15 min expiry)
// 2. Include in Authorization: Bearer {token}
// 3. Validate signature & expiration
// 4. Refresh via /api/auth/refresh (30 day expiry)
// 5. Logout → Blacklist token (Redis)
```

## 11.4 Password Policies (Future)

When implementing user authentication:
- Minimum 12 characters
- Must contain uppercase, lowercase, number, special character
- Cannot contain common passwords (use zxcvbn library)
- Hash with bcrypt (cost factor 12)
- Salt automatically generated per password
- Password history: prevent reuse of last 5 passwords
- Account lockout: 5 failed attempts → 15 minute lockout

## 11.5 Data Encryption

### In Transit
- **TLS 1.2+** required for all API communication
- **Certificate pinning** in mobile apps (future)
- **HSTS** header enabled (Strict-Transport-Security)

### At Rest
- **Transparent Data Encryption (TDE)** for SQL Server databases
- **Azure Key Vault** for sensitive configuration (production)
- **Connection strings** encrypted in appsettings (future)

### Sensitive Data Handling
```csharp
// Example: Encrypt connection strings before storage
public class EncryptionService
{
    private readonly byte[] _key;
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(
            Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);
        
        return Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(encrypted);
    }
}
```

## 11.6 Secrets Management

**Development**
```json
// appsettings.Local.json (gitignored)
{
  "OpenAI": {
    "ApiKey": "sk-..."
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=..."
  }
}
```

**Production**
```bash
# Azure Key Vault
az keyvault secret set --vault-name "aiquery-kv" --name "OpenAI--ApiKey" --value "sk-..."
az keyvault secret set --vault-name "aiquery-kv" --name "ConnectionStrings--DefaultConnection" --value "Server=..."

# App Service configuration
az webapp config appsettings set --name "aiquery-api" --resource-group "aiquery-rg" \
  --settings "OpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=https://aiquery-kv.vault.azure.net/secrets/OpenAI--ApiKey/)"
```

## 11.7 API Security

### CORS Policy
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://app.acme.com",
            "https://dashboard.acme.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

### Request Validation
```csharp
public class QueryRequest
{
    [Required(ErrorMessage = "Query is required")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Query must be 3-500 characters")]
    public string Query { get; set; }
    
    [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Invalid tenant ID format")]
    public string? TenantId { get; set; }
}
```

### OWASP Top 10 Mitigations

| Vulnerability | Mitigation |
|---------------|-----------|
| **A01: Broken Access Control** | Tenant isolation via TenantContext, database-level separation |
| **A02: Cryptographic Failures** | TLS 1.2+, TDE, Key Vault for secrets |
| **A03: Injection** | Parameterized queries, keyword filtering, pattern detection |
| **A04: Insecure Design** | Threat modeling, security reviews, layered validation |
| **A05: Security Misconfiguration** | Hardened defaults, minimal privileges, error sanitization |
| **A06: Vulnerable Components** | Dependabot alerts, regular updates, SCA scanning |
| **A07: Authentication Failures** | API key validation, rate limiting (future: MFA) |
| **A08: Software/Data Integrity** | Code signing, integrity checks, audit logs |
| **A09: Logging Failures** | Structured logging with Serilog, centralized monitoring |
| **A10: Server-Side Request Forgery** | No user-controlled URLs, allowlist for external calls |

---

# 12. Performance Considerations

## 12.1 Caching Strategy

### Multi-Level Caching
```
┌──────────────────────────────────────┐
│       Application Level Cache        │
│  • Schema (1h TTL)                   │
│  • Rate limit counters (1m-1h TTL)   │
│  • IMemoryCache (in-process)         │
└──────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────┐
│     Distributed Cache (Future)       │
│  • Redis for shared state            │
│  • Multi-instance deployments        │
│  • Session persistence               │
└──────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────┐
│         CDN Cache (Future)           │
│  • Static assets (widget JS/CSS)    │
│  • Azure CDN or Cloudflare           │
│  • Edge caching for global access    │
└──────────────────────────────────────┘
```

### Cache Implementation
```csharp
public class SchemaService
{
    public async Task<SchemaResponse> GetSchemaAsync()
    {
        var cacheKey = $"schema:{_tenantContext.TenantId}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            entry.Priority = CacheItemPriority.High;
            
            _logger.LogInformation("Cache miss for schema, fetching from database");
            return await DiscoverSchemaAsync();
        });
    }
}
```

## 12.2 Database Optimization

### Connection Pooling
```csharp
var connectionString = "Server=...;Database=...;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30";
```

### Query Optimization Techniques
1. **Indexing Strategy**
   ```sql
   -- Frequently queried columns
   CREATE INDEX IX_Tenants_ApiKey ON Tenants(ApiKey);
   CREATE INDEX IX_Conversations_TenantId_CreatedAt ON Conversations(TenantId, CreatedAt DESC);
   CREATE INDEX IX_TokenUsage_TenantId_CreatedAt ON TokenUsage(TenantId, CreatedAt DESC);
   ```

2. **Query Hints**
   ```sql
   -- Force index usage for tenant queries
   SELECT * FROM Tenants WITH (INDEX(IX_Tenants_ApiKey)) WHERE ApiKey = @key;
   ```

3. **Execution Plan Analysis**
   ```sql
   SET STATISTICS IO ON;
   SET STATISTICS TIME ON;
   
   -- Analyze query performance
   EXPLAIN SELECT TOP 100 * FROM Orders WHERE CustomerId = @id;
   ```

### Database Partitioning (Future)
```sql
-- Partition TokenUsage by date for improved query performance
CREATE PARTITION FUNCTION PF_TokenUsage_Date (DATETIME2)
AS RANGE RIGHT FOR VALUES 
  ('2026-01-01', '2026-02-01', '2026-03-01', ...);

CREATE PARTITION SCHEME PS_TokenUsage_Date
AS PARTITION PF_TokenUsage_Date
TO ([PRIMARY], [PRIMARY], [PRIMARY], ...);

CREATE TABLE TokenUsage (
    ...
    CreatedAt DATETIME2 NOT NULL
) ON PS_TokenUsage_Date(CreatedAt);
```

## 12.3 API Pagination

**Implementation**
```csharp
public class ConversationController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Conversation>>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100; // Max 100 items per page
        
        var query = _context.Conversations
            .Where(c => c.TenantId == _tenantContext.TenantId)
            .OrderByDescending(c => c.CreatedAt);
        
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PagedResult<Conversation>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
```

**Response Format**
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasNext": true,
  "hasPrevious": false
}
```

## 12.4 Lazy Loading & Code Splitting

**Widget Lazy Loading**
```typescript
// loader.ts (5KB)
(async () => {
  const config = extractConfig();
  
  // Load React bundle on first interaction
  const { default: loadWidget } = await import('./widget-app.js');
  loadWidget(config);
})();
```

**Webpack Code Splitting**
```javascript
// webpack.config.js
module.exports = {
  optimization: {
    splitChunks: {
      chunks: 'all',
      cacheGroups: {
        vendors: {
          test: /[\\/]node_modules[\\/]/,
          name: 'vendors',
          priority: 10
        },
        charts: {
          test: /[\\/]node_modules[\\/]chart\.js/,
          name: 'charts',
          priority: 20
        }
      }
    }
  }
};
```

## 12.5 Performance Metrics

**Target SLAs**

| Metric | Target | Measurement |
|--------|--------|-------------|
| **API Response Time (P95)** | < 500ms | Application Insights |
| **Query Execution (P95)** | < 5s | Custom telemetry |
| **OpenAI API Call (P95)** | < 3s | Custom telemetry |
| **Widget Initial Load** | < 2s | Lighthouse / RUM |
| **Database Query (P95)** | < 200ms | SQL Server DMVs |
| **Cache Hit Rate** | > 90% | IMemoryCache stats |
| **Error Rate** | < 1% | Application Insights |
| **Availability** | 99.5% | Azure Monitor |

**Performance Testing Script**
```bash
# Load testing with Apache Bench
ab -n 1000 -c 10 -H "x-api-key: demo_api_key_12345" \
   -p query.json -T application/json \
   https://api.aiquery.com/api/query/execute

# Load testing with k6
k6 run --vus 50 --duration 5m load-test.js
```

---

# 13. Deployment Architecture

## 13.1 Environments

### DEV Environment
- **Purpose**: Active development and feature testing
- **Infrastructure**: Single Azure App Service (B1)
- **Database**: Azure SQL Database (Basic tier)
- **OpenAI**: Shared development instance
- **URL**: https://dev-api.aiquery.com
- **Deployment**: Manual via Visual Studio / CLI
- **Data**: Synthetic test data

### UAT Environment
- **Purpose**: User acceptance testing and pre-production validation
- **Infrastructure**: Azure App Service (S1)
- **Database**: Azure SQL Database (S0 tier)
- **OpenAI**: Dedicated UAT instance
- **URL**: https://uat-api.aiquery.com
- **Deployment**: CI/CD via Azure DevOps
- **Data**: Anonymized production data

### PROD Environment
- **Purpose**: Live production system
- **Infrastructure**: Azure App Service (P1V2) with auto-scale
- **Database**: Azure SQL Database (S3 tier) with geo-replication
- **OpenAI**: Production instance with high quota
- **URL**: https://api.aiquery.com
- **Deployment**: Blue-green deployment via Azure DevOps
- **Data**: Real customer data

## 13.2 Infrastructure Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Front Door                         │
│  • SSL termination                                          │
│  • WAF (Web Application Firewall)                           │
│  • Global load balancing                                    │
│  • CDN for static assets                                    │
└───────────────────────┬─────────────────────────────────────┘
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼──────────┐         ┌──────────▼─────────┐
│  App Service 1   │         │  App Service 2     │
│  (Primary)       │         │  (Secondary)       │
│  • .NET 8        │         │  • .NET 8          │
│  • Always On     │         │  • Always On       │
│  • Auto-scale    │         │  • Auto-scale      │
└───────┬──────────┘         └──────────┬─────────┘
        │                               │
        └───────────────┬───────────────┘
                        │
        ┌───────────────▼───────────────┐
        │                               │
┌───────▼──────────────┐    ┌───────────▼────────┐
│  Azure SQL (Primary) │───►│  Azure SQL (Geo)   │
│  • S3 tier           │    │  • Read replica    │
│  • TDE enabled       │    │  • Failover group  │
│  • Backups (7 days)  │    │                    │
└──────────────────────┘    └────────────────────┘
        │
        │
┌───────▼──────────────┐
│  Azure Key Vault     │
│  • API keys          │
│  • Connection strings│
│  • Certificates      │
└──────────────────────┘
        │
        │
┌───────▼──────────────┐
│  Azure OpenAI        │
│  • GPT-4 deployment  │
│  • Private endpoint  │
│  • High quota        │
└──────────────────────┘
        │
        │
┌───────▼──────────────┐
│  Application Insights│
│  • Telemetry         │
│  • Distributed trace │
│  • Alerts            │
└──────────────────────┘
```

## 13.3 CI/CD Pipeline

### Build Pipeline (Azure DevOps)
```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildAPI
    steps:
    - task: UseDotNet@2
      inputs:
        version: '8.0.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore'
      inputs:
        command: restore
        projects: '**/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build'
      inputs:
        command: build
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'
    
    - task: DotNetCoreCLI@2
      displayName: 'Run Unit Tests'
      inputs:
        command: test
        projects: '**/*Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage"'
    
    - task: PublishCodeCoverageResults@1
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
    
    - task: DotNetCoreCLI@2
      displayName: 'Publish'
      inputs:
        command: publish
        projects: 'src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj'
        arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
        zipAfterPublish: true
    
    - task: PublishBuildArtifacts@1
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'drop'

- stage: SecurityScan
  dependsOn: Build
  jobs:
  - job: SAST
    steps:
    - task: SonarQubePrepare@5
      inputs:
        SonarQube: 'SonarQube Server'
        scannerMode: 'MSBuild'
        projectKey: 'AIQueryPlatform'
    
    - task: DotNetCoreCLI@2
      inputs:
        command: build
    
    - task: SonarQubeAnalyze@5
```

### Release Pipeline
```yaml
stages:
- stage: DeployToUAT
  jobs:
  - deployment: DeployUAT
    environment: 'UAT'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure Subscription'
              appType: 'webApp'
              appName: 'aiquery-uat'
              package: '$(Pipeline.Workspace)/drop/*.zip'
              deploymentMethod: 'zipDeploy'
          
          - task: AzureCLI@2
            displayName: 'Run Smoke Tests'
            inputs:
              azureSubscription: 'Azure Subscription'
              scriptType: 'bash'
              scriptLocation: 'inlineScript'
              inlineScript: |
                curl -f https://uat-api.aiquery.com/health || exit 1

- stage: DeployToPROD
  dependsOn: DeployToUAT
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployPROD
    environment: 'PROD'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureAppServiceManage@0
            displayName: 'Swap to Staging Slot'
            inputs:
              azureSubscription: 'Azure Subscription'
              action: 'Swap Slots'
              webAppName: 'aiquery-prod'
              resourceGroupName: 'aiquery-rg'
              sourceSlot: 'staging'
              targetSlot: 'production'
          
          - task: AzureCLI@2
            displayName: 'Verify Deployment'
            inputs:
              azureSubscription: 'Azure Subscription'
              scriptType: 'bash'
              scriptLocation: 'inlineScript'
              inlineScript: |
                # Health check
                curl -f https://api.aiquery.com/health || exit 1
                
                # Basic API test
                response=$(curl -s -H "x-api-key: $API_KEY" \
                  https://api.aiquery.com/api/query/schema)
                echo $response | jq -e '.tables | length > 0' || exit 1
```

## 13.4 Rollback Strategy

### Automated Rollback Triggers
- Health check failure > 5 minutes
- Error rate > 5%
- P95 latency > 10 seconds
- Manual rollback via approval gate

### Rollback Procedures

**Option 1: Slot Swap Rollback**
```bash
# Swap back to previous version
az webapp deployment slot swap \
  --name aiquery-prod \
  --resource-group aiquery-rg \
  --slot staging \
  --target-slot production \
  --action swap
```

**Option 2: Redeploy Previous Version**
```bash
# Redeploy last known good build
az pipelines run \
  --name "AIQueryPlatform-Release" \
  --branch main \
  --variables "buildId=12345"
```

**Option 3: Database Rollback**
```sql
-- Restore database to point-in-time
RESTORE DATABASE AIQueryPlatform
FROM DATABASE_SNAPSHOT = 'AIQueryPlatform_20260530_0900';
```

---

# 14. Test Strategy

## 14.1 Unit Test Cases

| Test ID | Module | Scenario | Input | Expected Result |
|---------|--------|----------|-------|-----------------|
| UT-001 | SqlValidatorService | Validate SELECT query | `SELECT * FROM Customers` | IsValid=true, SanitizedSql includes TOP 100 |
| UT-002 | SqlValidatorService | Block INSERT query | `INSERT INTO Customers ...` | IsValid=false, Error="Non-SELECT queries not allowed" |
| UT-003 | SqlValidatorService | Detect SQL injection | `SELECT * FROM Users WHERE id=1; DROP TABLE Users--` | IsValid=false, Error="Potential SQL injection" |
| UT-004 | IntelligenceLayerService | Detect chart for numeric data | Result with 2+ numeric columns | visualizationType="chart" |
| UT-005 | IntelligenceLayerService | Detect table for non-numeric | Result with text columns only | visualizationType="table" |
| UT-006 | NLToSqlService | Convert simple query | "Show all customers" | Valid SQL: `SELECT * FROM Customers` |
| UT-007 | QueryExecutionService | Execute valid SQL | `SELECT TOP 10 * FROM Orders` | QueryResult with columns and rows |
| UT-008 | SchemaService | Cache schema | First call to GetSchemaAsync() | Cache miss, DB query executed |
| UT-009 | SchemaService | Return cached schema | Second call within 1 hour | Cache hit, no DB query |
| UT-010 | TenantService | Get tenant by API key | Valid API key | Tenant object returned |
| UT-011 | TenantService | Invalid API key | Invalid API key | null returned |
| UT-012 | RateLimitingMiddleware | Allow under limit | 10 requests in 1 minute | All requests pass |
| UT-013 | RateLimitingMiddleware | Block over limit | 61 requests in 1 minute | 61st request returns 429 |

**Example Unit Test**
```csharp
[Fact]
public async Task ValidateAsync_SelectQuery_ReturnsValid()
{
    // Arrange
    var validator = new SqlValidatorService();
    var sql = "SELECT * FROM Customers";
    
    // Act
    var result = await validator.ValidateAsync(sql);
    
    // Assert
    Assert.True(result.IsValid);
    Assert.Contains("TOP 100", result.SanitizedSql);
}

[Fact]
public async Task ValidateAsync_InsertQuery_ReturnsInvalid()
{
    // Arrange
    var validator = new SqlValidatorService();
    var sql = "INSERT INTO Customers (Name) VALUES ('Test')";
    
    // Act
    var result = await validator.ValidateAsync(sql);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("Non-SELECT", result.ErrorMessage);
}
```

## 14.2 Integration Test Cases

### API Testing
| Test ID | Endpoint | Scenario | Expected Result |
|---------|----------|----------|-----------------|
| IT-001 | POST /api/query/execute | Valid query with valid API key | 200 OK, query results returned |
| IT-002 | POST /api/query/execute | Valid query with invalid API key | 401 Unauthorized |
| IT-003 | POST /api/query/execute | Malicious SQL query | 400 Bad Request, validation error |
| IT-004 | POST /api/query/execute-stream | Valid query with streaming | NDJSON events received |
| IT-005 | GET /api/query/schema | Valid API key | 200 OK, schema returned |
| IT-006 | POST /api/tenant | Create tenant as admin | 201 Created, tenant object |
| IT-007 | GET /api/conversations | Get conversation history | 200 OK, paginated results |

**Example Integration Test**
```csharp
[Fact]
public async Task ExecuteQuery_ValidRequest_ReturnsResults()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("x-api-key", "demo_api_key_12345");
    
    var request = new QueryRequest
    {
        Query = "Show top 5 customers"
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/query/execute", request);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<QueryResponse>();
    Assert.NotNull(result.Data);
    Assert.True(result.Data.Rows.Count <= 5);
}
```

### Database Testing
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| IT-DB-001 | Insert tenant with valid data | Tenant created, GUID generated |
| IT-DB-002 | Insert tenant with duplicate API key | Unique constraint violation |
| IT-DB-003 | Delete tenant with conversations | Cascade delete removes conversations |
| IT-DB-004 | Query INFORMATION_SCHEMA | Schema metadata returned |

### Authentication Testing
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| IT-AUTH-001 | Request without x-api-key | 401 Unauthorized |
| IT-AUTH-002 | Request with invalid x-api-key | 401 Unauthorized |
| IT-AUTH-003 | Request with valid but inactive tenant | 401 Unauthorized |
| IT-AUTH-004 | Request with valid active tenant | TenantContext populated |

### External Service Testing
| Test ID | Service | Scenario | Expected Result |
|---------|---------|----------|-----------------|
| IT-EXT-001 | Azure OpenAI | Valid prompt | SQL response returned |
| IT-EXT-002 | Azure OpenAI | Network timeout | Retry 3 times, then fail |
| IT-EXT-003 | Azure OpenAI | Invalid API key | OpenAIException thrown |
| IT-EXT-004 | Tenant SQL Server | Valid query | Results returned |
| IT-EXT-005 | Tenant MySQL | Valid query | Results returned |

## 14.3 Frontend Test Cases

### Component Tests (React)
```typescript
describe('QueryBar Component', () => {
  it('renders input field', () => {
    render(<QueryBar value="" onChange={jest.fn()} onSubmit={jest.fn()} />);
    expect(screen.getByPlaceholderText(/enter your question/i)).toBeInTheDocument();
  });
  
  it('calls onSubmit when Enter is pressed', () => {
    const onSubmit = jest.fn();
    render(<QueryBar value="test query" onChange={jest.fn()} onSubmit={onSubmit} />);
    
    fireEvent.keyDown(screen.getByRole('textbox'), { key: 'Enter' });
    
    expect(onSubmit).toHaveBeenCalledWith('test query');
  });
  
  it('disables submit when query is empty', () => {
    render(<QueryBar value="" onChange={jest.fn()} onSubmit={jest.fn()} disabled={false} />);
    expect(screen.getByRole('button')).toBeDisabled();
  });
});
```

### Form Validation Tests
| Test ID | Component | Scenario | Expected Result |
|---------|-----------|----------|-----------------|
| FE-VAL-001 | QueryBar | Submit empty query | Submit button disabled |
| FE-VAL-002 | QueryBar | Query < 3 characters | Validation error shown |
| FE-VAL-003 | QueryBar | Query > 500 characters | Validation error shown |
| FE-VAL-004 | TenantForm | Invalid email format | Email validation error |
| FE-VAL-005 | TenantForm | Empty required fields | Form submission blocked |

### Navigation Tests
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| FE-NAV-001 | Click floating button | Widget panel expands |
| FE-NAV-002 | Click close button | Widget panel collapses |
| FE-NAV-003 | Navigate between tabs | Active tab highlighted |
| FE-NAV-004 | Click suggested query | Query populated in input |

### State Management Tests
```typescript
describe('WidgetContext', () => {
  it('initializes with correct default state', () => {
    const { result } = renderHook(() => useWidget(), { wrapper: WidgetProvider });
    
    expect(result.current.state.view).toBe('collapsed');
    expect(result.current.state.isExpanded).toBe(false);
    expect(result.current.state.results).toBeNull();
  });
  
  it('updates state when executeQuery is called', async () => {
    const { result } = renderHook(() => useWidget(), { wrapper: WidgetProvider });
    
    await act(async () => {
      await result.current.actions.executeQuery('test query');
    });
    
    expect(result.current.state.view).toBe('results');
    expect(result.current.state.results).not.toBeNull();
  });
});
```

## 14.4 End-to-End Test Cases

### User Registration Flow
```gherkin
Feature: User Registration
  Scenario: Successful tenant registration
    Given I am an administrator
    When I navigate to the tenant management page
    And I fill in the tenant form with valid data
    And I click the "Create Tenant" button
    Then the tenant is created successfully
    And I see a success message
    And the tenant appears in the tenant list
```

### Query Execution Flow
```gherkin
Feature: Query Execution
  Scenario: Execute natural language query
    Given I am logged in with a valid API key
    When I open the query widget
    And I enter "Show me top 10 customers"
    And I click the submit button
    Then I see a loading indicator
    And I receive the SQL query generated
    And I see the query results in a table
    And the results contain at most 10 rows
```

### Error Handling Flow
```gherkin
Feature: Error Handling
  Scenario: Handle invalid SQL generation
    Given I am logged in with a valid API key
    When I enter a malicious query "DROP TABLE Customers"
    And I submit the query
    Then I see an error message
    And the error message says "Non-SELECT queries are not allowed"
    And no database modifications occur
```

## 14.5 Security Test Cases

### SQL Injection Tests
| Test ID | Attack Vector | Expected Result |
|---------|---------------|-----------------|
| SEC-001 | `'; DROP TABLE Customers--` | Blocked by validator |
| SEC-002 | `1' OR '1'='1` | Blocked by pattern detection |
| SEC-003 | `UNION SELECT * FROM Users` | Blocked by keyword filter |
| SEC-004 | `'; EXEC xp_cmdshell 'dir'--` | Blocked by keyword filter |
| SEC-005 | `/**/SELECT/**/password/**/FROM/**/Users` | Blocked by comment removal |

### XSS Tests
| Test ID | Attack Vector | Expected Result |
|---------|---------------|-----------------|
| SEC-XSS-001 | `<script>alert('XSS')</script>` in query | HTML escaped in response |
| SEC-XSS-002 | `<img src=x onerror=alert('XSS')>` | HTML escaped in response |
| SEC-XSS-003 | `javascript:alert('XSS')` | Sanitized before rendering |

### Authentication Bypass Tests
| Test ID | Attack Vector | Expected Result |
|---------|---------------|-----------------|
| SEC-AUTH-001 | Request without x-api-key header | 401 Unauthorized |
| SEC-AUTH-002 | Request with malformed API key | 401 Unauthorized |
| SEC-AUTH-003 | Request with expired API key (future) | 401 Unauthorized |
| SEC-AUTH-004 | Attempt to access another tenant's data | 403 Forbidden |

### Authorization Validation Tests
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| SEC-AUTHZ-001 | User A queries Tenant A data | Success |
| SEC-AUTHZ-002 | User A queries Tenant B data | 403 Forbidden |
| SEC-AUTHZ-003 | Admin creates tenant | Success |
| SEC-AUTHZ-004 | Regular user creates tenant | 403 Forbidden (future) |

## 14.6 Performance Test Cases

### Load Testing
```javascript
// k6 load test script
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 50 },  // Ramp up to 50 users
    { duration: '5m', target: 50 },  // Stay at 50 users
    { duration: '2m', target: 100 }, // Ramp up to 100 users
    { duration: '5m', target: 100 }, // Stay at 100 users
    { duration: '2m', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'], // 95% of requests < 5s
    http_req_failed: ['rate<0.01'],    // Error rate < 1%
  },
};

export default function () {
  let response = http.post(
    'https://api.aiquery.com/api/query/execute',
    JSON.stringify({ query: 'Show top 10 customers' }),
    {
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': 'demo_api_key_12345',
      },
    }
  );
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 5s': (r) => r.timings.duration < 5000,
  });
  
  sleep(1);
}
```

### Stress Testing
| Test ID | Scenario | Target | Expected Result |
|---------|----------|--------|-----------------|
| PERF-STR-001 | Concurrent queries | 200 simultaneous | System handles gracefully |
| PERF-STR-002 | Large result sets | Query returning 10K rows | Completes within 10s |
| PERF-STR-003 | Database connection exhaustion | 150 concurrent DB connections | Connection pooling prevents errors |

### Spike Testing
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| PERF-SPK-001 | Traffic spike from 10 to 500 users in 30s | Auto-scaling kicks in within 2 min |
| PERF-SPK-002 | Sudden drop from 500 to 10 users | System scales down gracefully |

### Volume Testing
| Test ID | Scenario | Expected Result |
|---------|----------|-----------------|
| PERF-VOL-001 | 10M conversations in database | Query performance remains acceptable |
| PERF-VOL-002 | 1M token usage records per tenant | Reporting queries complete within 3s |

---

# 15. Traceability Matrix

## 15.1 Requirements to Implementation Mapping

| Business Req | Functional Req | API Endpoint | Database | Test Cases |
|--------------|----------------|--------------|----------|------------|
| **BR-001: Natural Language Querying** | FR-001, FR-002 | POST /api/query/execute-stream, POST /api/query/execute | Tenants, Conversations | UT-006, IT-001, E2E-001 |
| **BR-002: Multi-Tenant Isolation** | FR-009, FR-011, FR-012 | All endpoints with x-api-key | Tenants (isolated DBs) | UT-010, IT-AUTH-004, SEC-AUTHZ-002 |
| **BR-003: Security & Validation** | FR-015, FR-016, FR-017 | N/A (SqlValidatorService) | N/A | UT-001-003, SEC-001-005 |
| **BR-004: Real-Time Streaming** | FR-003 | POST /api/query/execute-stream | N/A | IT-004 |
| **BR-005: Visualization Recommendations** | FR-005 | (Intelligence Layer) | N/A | UT-004, UT-005 |
| **BR-006: PDF Reporting** | FR-021-025 | POST /api/query/generate-report | Tenants | IT-006 |
| **BR-007: Usage Analytics** | FR-026-028 | GET /api/tokenusage | TokenUsage | IT-007, UT-008 |
| **BR-008: Query History** | FR-006, FR-029 | GET /api/conversations | Conversations | IT-007 |
| **BR-009: Rate Limiting** | FR-019 | N/A (Middleware) | N/A | UT-012-013 |
| **BR-010: Multi-Database Support** | FR-008 | All query endpoints | Tenants.DatabaseType | IT-DB-004, IT-EXT-005 |

## 15.2 Feature to Test Coverage Matrix

| Feature | Unit Tests | Integration Tests | E2E Tests | Security Tests | Performance Tests |
|---------|------------|-------------------|-----------|----------------|-------------------|
| **NL2SQL Conversion** | ✅ UT-006 | ✅ IT-EXT-001 | ✅ E2E-001 | ❌ | ✅ PERF-001 |
| **SQL Validation** | ✅ UT-001-003 | ✅ IT-003 | ✅ E2E-002 | ✅ SEC-001-005 | ❌ |
| **Query Execution** | ✅ UT-007 | ✅ IT-001 | ✅ E2E-001 | ✅ SEC-AUTHZ-001-002 | ✅ PERF-001-003 |
| **Streaming** | ❌ | ✅ IT-004 | ✅ E2E-001 | ❌ | ✅ PERF-001 |
| **Tenant Management** | ✅ UT-010-011 | ✅ IT-006 | ✅ E2E-003 | ✅ SEC-AUTHZ-003-004 | ❌ |
| **Rate Limiting** | ✅ UT-012-013 | ✅ IT-008 | ❌ | ❌ | ✅ PERF-STR-001 |
| **Schema Discovery** | ✅ UT-008-009 | ✅ IT-005 | ❌ | ❌ | ❌ |
| **PDF Generation** | ❌ | ✅ IT-006 | ❌ | ❌ | ❌ |
| **Widget** | ✅ FE-001-005 | ❌ | ✅ E2E-001 | ✅ SEC-XSS-001-003 | ❌ |

---

# 16. Risks and Assumptions

## 16.1 Technical Risks

### High-Priority Risks

| Risk ID | Risk | Impact | Probability | Mitigation Strategy |
|---------|------|--------|-------------|---------------------|
| RISK-001 | **Azure OpenAI Service Outage** | **Critical** - Core NL2SQL feature unavailable | Medium | • Implement circuit breaker pattern<br>• Fallback to OpenAI API<br>• Cache common query patterns<br>• Display clear error message to users |
| RISK-002 | **SQL Injection Vulnerability** | **Critical** - Data breach, compliance violation | Low | • Multi-layer validation (keywords, patterns, AST)<br>• Regular security audits<br>• Penetration testing<br>• Bug bounty program |
| RISK-003 | **Cross-Tenant Data Leak** | **Critical** - Regulatory violation, customer trust loss | Very Low | • Scoped TenantContext per request<br>• Database-level isolation<br>• Comprehensive integration tests<br>• Code reviews for all tenant logic |
| RISK-004 | **Database Connection Exhaustion** | **High** - Service unavailable | Medium | • Connection pooling (max 100)<br>• Query timeout enforcement (30s)<br>• Monitoring and alerting<br>• Auto-scaling App Service |
| RISK-005 | **Large Query Result OOM** | **High** - Application crashes | Medium | • Enforce row limits (100 default)<br>• Streaming results with IAsyncEnumerable<br>• Memory monitoring and alerts<br>• Pod/instance auto-restart |

### Medium-Priority Risks

| Risk ID | Risk | Impact | Probability | Mitigation Strategy |
|---------|------|--------|-------------|---------------------|
| RISK-006 | **Token Cost Explosion** | **Medium** - Budget overrun | Medium | • Track token usage per tenant<br>• Implement per-tenant quotas<br>• Cost alerts in Azure<br>• Optimize prompts to reduce tokens |
| RISK-007 | **Schema Discovery Failure** | **Medium** - Inaccurate SQL generation | Low | • Schema caching (1-hour TTL)<br>• Fallback to basic schema<br>• Manual schema refresh endpoint<br>• Error handling and logging |
| RISK-008 | **Rate Limit Abuse** | **Medium** - Service degradation | Medium | • Per-tenant rate limiting (60/min, 1000/hr)<br>• IP-based rate limiting (future)<br>• CAPTCHA for excessive failures (future)<br>• Monitoring and automatic blocking |
| RISK-009 | **PDF Generation Memory** | **Medium** - OOM during PDF creation | Low | • Limit PDF to 10K rows<br>• Stream PDF directly to response<br>• Memory monitoring<br>• Optimize iTextSharp usage |
| RISK-010 | **CORS Misconfiguration** | **Medium** - Widget unusable | Low | • Explicit origin allowlist<br>• Test across multiple domains<br>• Environment-specific CORS config<br>• Documentation for customers |

### Low-Priority Risks

| Risk ID | Risk | Impact | Probability | Mitigation Strategy |
|---------|------|--------|-------------|---------------------|
| RISK-011 | **Stale Schema Cache** | **Low** - Incorrect SQL for new tables | Medium | • 1-hour cache TTL<br>• Manual cache clear endpoint<br>• Auto-refresh on validation errors<br>• Document cache behavior |
| RISK-012 | **Browser Compatibility** | **Low** - Widget not working in old browsers | Low | • Target modern browsers (Chrome 90+, Firefox 88+)<br>• Polyfills for critical features<br>• Graceful degradation<br>• Browser support documentation |
| RISK-013 | **Dependency Vulnerabilities** | **Low** - Security vulnerabilities in packages | Medium | • Dependabot alerts enabled<br>• Regular dependency updates<br>• SCA scanning in CI/CD<br>• Quarterly security reviews |

## 16.2 Dependencies

### External Service Dependencies

| Dependency | Criticality | Failure Impact | SLA | Mitigation |
|------------|-------------|----------------|-----|------------|
| **Azure OpenAI** | Critical | No SQL generation | 99.9% | Fallback to OpenAI API |
| **OpenAI API** | High | Fallback provider down | 99.9% | Cached query patterns, error messaging |
| **Tenant SQL Databases** | Critical | Query execution fails | 99.99% (Azure SQL) | Connection retry, health checks |
| **Platform Database** | Critical | No tenant resolution | 99.99% (Azure SQL) | Geo-replication, automatic failover |
| **Azure Key Vault** | High | Cannot load secrets | 99.99% | Cached secrets in App Service config |
| **Application Insights** | Medium | No telemetry | 99.9% | Serilog file logging as backup |

### Technology Dependencies

| Technology | Version | End of Support | Upgrade Plan |
|------------|---------|----------------|--------------|
| **.NET** | 8.0 | Nov 2026 | Migrate to .NET 9 by Q3 2026 |
| **React** | 18.x | No EOL | Monitor for React 19, plan upgrade |
| **SQL Server** | 2019+ | Jan 2030 | No immediate action |
| **Chart.js** | 4.x | Active | Monitor for breaking changes |
| **TypeScript** | 5.x | Active | Regular minor version updates |

## 16.3 Assumptions

### Business Assumptions
- **ASM-001**: Users have basic understanding of their database structure
- **ASM-002**: English language queries are sufficient for initial release
- **ASM-003**: Customers accept READ-ONLY access (no data modification)
- **ASM-004**: API key-based auth is acceptable (no user login required initially)
- **ASM-005**: 100-row result limit is sufficient for most queries
- **ASM-006**: Customers store API keys securely (not in client-side code)

### Technical Assumptions
- **ASM-007**: Azure OpenAI service available with sufficient quota
- **ASM-008**: Tenant databases accessible from API server (network connectivity)
- **ASM-009**: Modern browsers (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)
- **ASM-010**: Single Azure region deployment sufficient initially
- **ASM-011**: In-memory cache sufficient for single-instance deployment
- **ASM-012**: INFORMATION_SCHEMA queries work for schema discovery
- **ASM-013**: GPT-4 model provides sufficient accuracy for SQL generation (>90%)

### Data Assumptions
- **ASM-014**: Most queries return < 10,000 rows
- **ASM-015**: Schema changes are infrequent (1-hour cache acceptable)
- **ASM-016**: Tenant databases have standard schema naming conventions
- **ASM-017**: Query complexity is moderate (no highly complex analytics)

### Infrastructure Assumptions
- **ASM-018**: Azure App Service provides sufficient scalability
- **ASM-019**: Azure SQL Database S3 tier sufficient for platform database
- **ASM-020**: CDN not required for initial release (widget served from App Service)
- **ASM-021**: Single-region deployment meets latency requirements (<5s query)

---

# 17. Appendix

## 17.1 Glossary

| Term | Definition |
|------|------------|
| **API Key** | Unique authentication token assigned to each tenant for API access |
| **ASP.NET Core** | Cross-platform, high-performance framework for building modern web applications |
| **Circuit Breaker** | Design pattern that prevents cascading failures by detecting failures and stopping requests to failing services |
| **CORS** | Cross-Origin Resource Sharing - mechanism allowing restricted resources on a web page to be requested from another domain |
| **Dependency Injection (DI)** | Design pattern where dependencies are provided to a class rather than created by the class |
| **DTO (Data Transfer Object)** | Object that carries data between processes |
| **IAsyncEnumerable** | .NET interface for async streaming that produces a sequence of values asynchronously |
| **IMemoryCache** | In-memory cache implementation in .NET for storing frequently accessed data |
| **INFORMATION_SCHEMA** | Standard database views that provide metadata about database structure |
| **JWT (JSON Web Token)** | Compact, URL-safe means of representing claims to be transferred between two parties |
| **LLM (Large Language Model)** | AI model trained on large text datasets for natural language understanding |
| **NDJSON** | Newline Delimited JSON - streaming JSON format where each line is a valid JSON object |
| **NL2SQL** | Natural Language to SQL - process of converting natural language queries to SQL |
| **OWASP** | Open Web Application Security Project - organization focused on improving software security |
| **Rate Limiting** | Technique for limiting network traffic by restricting the number of requests |
| **Row Limit** | Maximum number of rows returned from a query to prevent memory issues |
| **Schema Discovery** | Runtime inspection of database structure to understand tables, columns, and relationships |
| **Scoped Service** | Service lifetime in DI where one instance is created per request |
| **Shadow DOM** | Web standard that encapsulates DOM and CSS, preventing leakage to host page |
| **SQL Injection** | Code injection technique used to attack data-driven applications |
| **SSE (Server-Sent Events)** | Standard allowing servers to push data to web clients over HTTP |
| **Streaming Response** | Progressive data delivery where results are sent as they're generated |
| **TDE (Transparent Data Encryption)** | Database encryption that encrypts data at rest |
| **Tenant** | Independent customer of the platform with isolated database and API key |
| **TenantContext** | Scoped service holding current tenant information for a request |
| **Token Usage** | Measurement of OpenAI API consumption in tokens (input + output) |
| **TTL (Time To Live)** | Duration for which data remains valid in cache |

## 17.2 Acronyms

| Acronym | Full Form |
|---------|-----------|
| **ADR** | Architectural Decision Record |
| **AI** | Artificial Intelligence |
| **API** | Application Programming Interface |
| **CDN** | Content Delivery Network |
| **CI/CD** | Continuous Integration / Continuous Deployment |
| **CORS** | Cross-Origin Resource Sharing |
| **CQRS** | Command Query Responsibility Segregation |
| **CRUD** | Create, Read, Update, Delete |
| **DI** | Dependency Injection |
| **DTO** | Data Transfer Object |
| **E2E** | End-to-End |
| **GPT** | Generative Pre-trained Transformer |
| **GUID** | Globally Unique Identifier |
| **HTTPS** | HyperText Transfer Protocol Secure |
| **JWT** | JSON Web Token |
| **LLM** | Large Language Model |
| **MFA** | Multi-Factor Authentication |
| **NDJSON** | Newline Delimited JSON |
| **NL2SQL** | Natural Language to SQL |
| **OOM** | Out Of Memory |
| **OWASP** | Open Web Application Security Project |
| **PDF** | Portable Document Format |
| **REST** | Representational State Transfer |
| **RUM** | Real User Monitoring |
| **SCA** | Software Composition Analysis |
| **SDK** | Software Development Kit |
| **SLA** | Service Level Agreement |
| **SQL** | Structured Query Language |
| **SRP** | Single Responsibility Principle |
| **SSE** | Server-Sent Events |
| **TDE** | Transparent Data Encryption |
| **TLS** | Transport Layer Security |
| **TSD** | Technical Specification Document |
| **TTL** | Time To Live |
| **UAT** | User Acceptance Testing |
| **UI** | User Interface |
| **UUID** | Universally Unique Identifier |
| **WAF** | Web Application Firewall |
| **WCAG** | Web Content Accessibility Guidelines |

## 17.3 References

### Documentation
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [React Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Chart.js Documentation](https://www.chartjs.org/docs/)
- [Serilog Documentation](https://serilog.net/)

### Standards & Best Practices
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [REST API Design Best Practices](https://restfulapi.net/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)

### Related Project Documents
- **PROJECT_SUMMARY.md** - Feature overview and implementation status
- **README.md** - Quick start guide and API documentation
- **CONFIGURATION.md** - Setup and configuration instructions
- **DEPLOYMENT.md** - Deployment procedures and environment setup
- **TOKEN_USAGE_IMPLEMENTATION_GUIDE.md** - Token tracking specifications
- **MULTI_DATABASE_SUPPORT.md** - Multi-database architecture details
- **widget/ARCHITECTURE.md** - Frontend widget architecture
- **widget/INTEGRATION.md** - Widget integration guide

## 17.4 Coding Standards

### C# Coding Standards
- **Naming Conventions**: PascalCase for classes, methods, properties; camelCase for local variables
- **File Organization**: One class per file, namespace matches folder structure
- **Comments**: XML documentation for all public APIs
- **Async**: Use async/await for all I/O operations, suffix async methods with "Async"
- **LINQ**: Prefer method syntax over query syntax
- **Error Handling**: Use specific exception types, avoid catching generic Exception
- **Dependency Injection**: Register services with appropriate lifetime (Scoped, Singleton, Transient)

**Example**
```csharp
/// <summary>
/// Executes a natural language query and returns results.
/// </summary>
/// <param name="query">The natural language query to execute.</param>
/// <returns>Query results with visualization type.</returns>
/// <exception cref="ValidationException">Thrown when query validation fails.</exception>
public async Task<QueryResponse> ExecuteQueryAsync(string query)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        throw new ValidationException("Query cannot be empty");
    }
    
    _logger.LogInformation("Executing query: {Query}", query);
    
    // Implementation...
}
```

### TypeScript/React Coding Standards
- **Naming Conventions**: PascalCase for components, camelCase for functions/variables
- **File Organization**: Component per file, co-locate styles and tests
- **Type Safety**: Explicit types for function parameters and return values
- **Functional Components**: Use hooks, avoid class components
- **Props**: Define interfaces for all component props
- **State**: Use useState for local state, useContext for global state
- **Async**: Use async/await, handle errors with try/catch

**Example**
```typescript
interface QueryBarProps {
  value: string;
  onChange: (value: string) => void;
  onSubmit: (query: string) => void;
  disabled?: boolean;
}

export const QueryBar: React.FC<QueryBarProps> = ({ 
  value, 
  onChange, 
  onSubmit, 
  disabled = false 
}) => {
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && value.trim()) {
      onSubmit(value);
    }
  };
  
  return (
    <input
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      onKeyDown={handleKeyDown}
      disabled={disabled}
      placeholder="Enter your question..."
    />
  );
};
```

### SQL Coding Standards
- **Naming**: PascalCase for tables and columns
- **Primary Keys**: TableNameId (e.g., CustomerId, OrderId)
- **Foreign Keys**: Same name as referenced primary key
- **Indexes**: IX_TableName_ColumnName
- **Constraints**: FK_TableName_ReferencedTable, CK_TableName_Condition
- **Formatting**: Keywords in UPPERCASE, indent clauses

**Example**
```sql
CREATE TABLE Customers (
    CustomerId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE '%@%.%')
);

CREATE INDEX IX_Customers_Email ON Customers(Email);
```

## 17.5 Version History

| Version | Date | Author | Changes | Approved By |
|---------|------|--------|---------|-------------|
| 0.1 | 2026-05-15 | Tech Lead | Initial draft | - |
| 0.5 | 2026-05-20 | Dev Team | Added sections 1-10 | Tech Lead |
| 0.8 | 2026-05-25 | QA Lead | Added test strategy | Tech Lead |
| 1.0 | 2026-05-30 | Development Team | Complete TSD ready for review | Product Manager |

## 17.6 Approvals

| Role | Name | Signature | Date |
|------|------|-----------|------|
| **Technical Lead** | ________________ | ________________ | __________ |
| **Product Manager** | ________________ | ________________ | __________ |
| **QA Manager** | ________________ | ________________ | __________ |
| **Security Officer** | ________________ | ________________ | __________ |
| **DevOps Lead** | ________________ | ________________ | __________ |

---

**END OF TECHNICAL SPECIFICATION DOCUMENT**

**Document Classification:** Internal - Confidential  
**Next Review Date:** November 30, 2026  
**Document Owner:** Development Team  
**Contact:** dev-team@aiquery.com
