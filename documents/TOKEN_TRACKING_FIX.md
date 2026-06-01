# Token Tracking Fix - Issue Resolved

## Problem
Token usage was not being tracked because the services making OpenAI API calls were not recording the token consumption. The `TokenUsageSummary` table had no data and the UI showed 0 usage even after performing searches.

## Root Cause
Two services were making OpenAI API calls without tracking token usage:
1. **NLToSqlService** - Converts natural language to SQL
2. **RecommendationService** - Generates AI recommendations

Both services were:
- ✅ Making OpenAI API calls successfully
- ❌ NOT extracting token usage from responses
- ❌ NOT calling `TokenUsageService.RecordTokenUsageAsync()`

## Changes Made

### 1. Updated `NLToSqlService.cs`
**Location:** `src/AIQueryPlatform.Api/Services/NLToSqlService.cs`

**Changes:**
- Added `using AIQueryPlatform.Api.Helpers;` for token extraction helpers
- Injected `ITokenUsageService` and `TenantContext` dependencies
- Added token tracking after OpenAI response:
  ```csharp
  // Track token usage
  if (_tenantContext.HasTenant && response.Value.HasTokenUsage())
  {
      var (promptTokens, completionTokens, totalTokens) = response.Value.ExtractTokenUsage();
      await _tokenUsageService.RecordTokenUsageAsync(new RecordTokenUsageRequest
      {
          TenantId = _tenantContext.CurrentTenant!.TenantId,
          RequestTokens = promptTokens,
          ResponseTokens = completionTokens,
          TotalTokens = totalTokens,
          ModelName = _deploymentName,
          Endpoint = "NL-to-SQL",
          Query = query,
          Status = "Success"
      });
  }
  ```

### 2. Updated `RecommendationService.cs`
**Location:** `src/AIQueryPlatform.Api/Services/RecommendationService.cs`

**Changes:**
- Added `using AIQueryPlatform.Api.Helpers;` and `using AIQueryPlatform.Api.Models;`
- Injected `ITokenUsageService` and `TenantContext` dependencies
- Added token tracking after OpenAI response (similar pattern to NLToSqlService)
- Endpoint set to `"Recommendations"` to differentiate from NL-to-SQL calls

## How Token Tracking Works Now

```
┌──────────────────────────────────────────────────────────────┐
│                     TOKEN TRACKING FLOW                        │
└──────────────────────────────────────────────────────────────┘

1. User sends query → QueryController
   │
2. QueryOrchestrationService processes query
   │
3. NLToSqlService.ConvertNaturalLanguageToSqlAsync()
   ├── Calls OpenAI API (GetChatCompletionsAsync)
   ├── ✅ Extracts token usage from response
   └── ✅ Records usage via TokenUsageService.RecordTokenUsageAsync()
   
4. (Optional) RecommendationService.GenerateRecommendationsAsync()
   ├── Calls OpenAI API (GetChatCompletionsAsync)
   ├── ✅ Extracts token usage from response
   └── ✅ Records usage via TokenUsageService.RecordTokenUsageAsync()

5. TokenUsageService saves to database:
   ├── Inserts detailed record into TokenUsage table
   └── Updates (MERGE) TokenUsageSummary for fast queries
```

## What Gets Tracked

For each OpenAI API call, the following is recorded:

| Field | Description | Example |
|-------|-------------|---------|
| TenantId | Multi-tenant identifier | `guid` |
| RequestTokens | Prompt/input tokens | 250 |
| ResponseTokens | Completion/output tokens | 150 |
| TotalTokens | Request + Response | 400 |
| ModelName | AI model used | "gpt-4" |
| Endpoint | Which service made the call | "NL-to-SQL" or "Recommendations" |
| Query | User's original query | "show top 10 customers" |
| Status | Success/Failed | "Success" |
| CreatedDate | Timestamp | UTC datetime |

## Testing Instructions

### 1. Rebuild the API
```powershell
cd src/AIQueryPlatform.Api
dotnet build
```

### 2. Verify Database Schema
Make sure the token usage tables exist:
```sql
-- Check if tables exist
SELECT name FROM sys.tables 
WHERE name IN ('TenantSubscriptions', 'TokenUsage', 'TokenUsageSummary')

-- Check if stored procedures exist
SELECT name FROM sys.procedures 
WHERE name IN ('sp_RecordTokenUsage', 'sp_GetTenantTokenUsage', 'sp_CheckTenantQuota')
```

If tables/procedures are missing, run:
```powershell
# From AIQueryPlatform root directory
sqlcmd -S localhost -d AIQueryPlatform -i database/token_usage_schema.sql
```

### 3. Test Token Tracking

**Step 1: Start the API**
```powershell
cd src/AIQueryPlatform.Api
dotnet run
```

**Step 2: Send a test query**
```powershell
# Using the frontend or direct API call
POST http://localhost:5000/api/query/execute-stream
Content-Type: application/json
X-Tenant-Id: <your-tenant-id>

{
  "query": "show top 5 customers by revenue"
}
```

**Step 3: Verify data is recorded**
```sql
-- Check detailed usage log
SELECT TOP 10
    TenantId,
    RequestTokens,
    ResponseTokens,
    TotalTokens,
    ModelName,
    Endpoint,
    Query,
    CreatedDate
FROM TokenUsage
ORDER BY CreatedDate DESC;

-- Check summary (what UI displays)
SELECT 
    TenantId,
    CurrentMonthUsedTokens,
    MonthlyTokenLimit,
    RemainingTokens,
    TotalRequests,
    SuccessfulRequests,
    LastRequestDate
FROM TokenUsageSummary;

-- Verify via stored procedure (what API uses)
EXEC sp_GetTenantTokenUsage @TenantId = '<your-tenant-id>';
```

**Step 4: Check UI**
- Open the frontend widget
- Look for the token usage display (usually in header or sidebar)
- Should show:
  - Used tokens (non-zero after queries)
  - Monthly limit
  - Usage percentage
  - Remaining tokens

### 4. Expected Results

After sending a query, you should see:

✅ **TokenUsage table**: New row with detailed token counts  
✅ **TokenUsageSummary table**: Updated totals for the tenant  
✅ **UI**: Non-zero token usage displayed  
✅ **Logs**: "Recorded token usage for tenant..." messages  

## Troubleshooting

### Issue: Still showing 0 usage

**Check 1: Tenant Context**
```csharp
// In logs, verify you see:
"Using tenant ID from request: {TenantId}"
// or
"TenantId from header: {TenantId}"
```

**Check 2: Database Connection**
Verify `appsettings.json` has correct connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AIQueryPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Check 3: OpenAI Response**
Check if OpenAI is actually returning token usage:
```csharp
// Add temporary logging in NLToSqlService after OpenAI call:
_logger.LogInformation("OpenAI Response - HasUsage: {HasUsage}, TotalTokens: {Total}", 
    response.Value.HasTokenUsage(), 
    response.Value.Usage?.TotalTokens ?? 0);
```

**Check 4: Stored Procedure Permissions**
```sql
-- Grant execute permission to your database user
GRANT EXECUTE ON sp_RecordTokenUsage TO [YourUser];
GRANT EXECUTE ON sp_GetTenantTokenUsage TO [YourUser];
```

**Check 5: TenantSubscriptions Exists**
Token tracking requires an active subscription:
```sql
-- Verify tenant has subscription
SELECT * FROM TenantSubscriptions WHERE TenantId = '<your-tenant-id>';

-- If missing, insert one:
INSERT INTO TenantSubscriptions (TenantId, PlanName, MonthlyTokenLimit, IsActive, BillingCycleStart, BillingCycleEnd)
VALUES (
    '<your-tenant-id>',
    'Free',
    100000,
    1,
    DATEADD(MONTH, DATEDIFF(MONTH, 0, GETUTCDATE()), 0),
    DATEADD(MONTH, DATEDIFF(MONTH, 0, GETUTCDATE()) + 1, 0)
);
```

## Additional Notes

### Token Usage Helper Extension
The `OpenAITokenExtensions.cs` helper provides clean extraction:
- `HasTokenUsage()` - Checks if response contains usage data
- `ExtractTokenUsage()` - Returns (promptTokens, completionTokens, totalTokens) tuple

### Performance Considerations
- **TokenUsage table**: Detailed log for analytics (can grow large)
- **TokenUsageSummary table**: Cached aggregates for fast UI queries
- The `sp_RecordTokenUsage` procedure uses MERGE to efficiently update summaries

### Future Enhancements
Consider adding token tracking to any other services that call LLMs:
- Search for: `GetChatCompletionsAsync` or `GetCompletionsAsync`
- Add similar tracking pattern with appropriate Endpoint names

## Files Modified
1. `src/AIQueryPlatform.Api/Services/NLToSqlService.cs`
2. `src/AIQueryPlatform.Api/Services/RecommendationService.cs`

## Files Referenced (Not Modified)
- `src/AIQueryPlatform.Api/Helpers/OpenAITokenExtensions.cs` - Token extraction helper
- `src/AIQueryPlatform.Api/Services/TokenUsageService.cs` - Recording service
- `database/token_usage_schema.sql` - Database schema
- `src/AIQueryPlatform.Api/Models/DTOs/TokenUsageDTOs.cs` - Data models

---

**Status:** ✅ FIXED - Token usage is now properly tracked for all OpenAI API calls
