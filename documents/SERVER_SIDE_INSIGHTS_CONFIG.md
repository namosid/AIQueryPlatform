# Server-Side Insights Configuration

## Overview

The insights/recommendations feature is now controlled **server-side** via tenant configuration. This prevents unnecessary AI token consumption by disabling recommendation generation at the API level when not needed.

## Architecture

```
┌────────────────────────────────────────────────────────────┐
│                   INSIGHTS CONTROL FLOW                     │
└────────────────────────────────────────────────────────────┘

1. TENANT CONFIGURATION (Database)
   ├─> Tenants table: EnableInsights column (BIT)
   └─> Default: TRUE (enabled)

2. API REQUEST
   ├─> User sends query via widget/modal
   └─> Backend receives request

3. BACKEND CHECK (ConversationsController)
   ├─> Check: tenant.EnableInsights?
   │
   ├─> IF TRUE:
   │   ├─> Call RecommendationService
   │   ├─> Generate AI recommendations (LLM call)
   │   └─> Consume tokens 💰
   │
   └─> IF FALSE:
       ├─> Skip RecommendationService completely
       ├─> No LLM call
       └─> Save tokens ✅

4. FRONTEND DISPLAY
   ├─> Widget fetches tenant.EnableInsights from API
   ├─> Modal checks insightsEnabled
   │
   ├─> IF TRUE: Show RightPanel with recommendations
   └─> IF FALSE: Hide RightPanel (no insights UI)
```

## Benefits

✅ **Token Savings**: No LLM calls for recommendations when disabled  
✅ **Centralized Control**: Managed at tenant level, not per-client  
✅ **Performance**: Faster query responses (no recommendation generation)  
✅ **Flexible**: Can enable/disable per tenant based on subscription tier  
✅ **Automatic**: Frontend fetches setting automatically  

## Database Changes

### New Column: `EnableInsights`

**File:** `database/add_enable_insights_column.sql`

```sql
ALTER TABLE Tenants
ADD EnableInsights BIT NOT NULL DEFAULT 1;
```

**Default:** `1` (TRUE) - Insights enabled for all existing tenants

### Migration

Run the migration script:

```powershell
# Connect to your database
sqlcmd -S localhost -d AIQueryPlatform -i database/add_enable_insights_column.sql
```

Or using SQL Server Management Studio:
1. Open the script
2. Execute against `AIQueryPlatform` database

## Backend Implementation

### 1. Tenant Model

**File:** `src/AIQueryPlatform.Api/Models/Tenant.cs`

```csharp
public class Tenant
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    public string? DatabaseSettings { get; set; }
    public string? LogoUrl { get; set; }
    public string? ThemeColor { get; set; }
    
    // Feature flags
    public bool EnableInsights { get; set; } = true; // NEW
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Conditional Recommendation Generation

**File:** `src/AIQueryPlatform.Api/Controllers/ConversationsController.cs`

```csharp
// Step 3: Generate AI recommendations asynchronously (only if enabled for tenant)
List<RecommendationDto>? recommendations = null;

// Check if insights/recommendations are enabled for this tenant
if (_tenantContext.CurrentTenant!.EnableInsights)
{
    try
    {
        if (queryResponse.Result != null && queryResponse.Result.RowCount > 0)
        {
            _logger.LogInformation("Generating AI recommendations for query results (insights enabled for tenant)");
            recommendations = await _recommendationService.GenerateRecommendationsAsync(
                request.Query,
                queryResponse.Result,
                schemaContext: null
            );
            _logger.LogInformation("Generated {Count} recommendations", recommendations?.Count ?? 0);
        }
    }
    catch (Exception recEx)
    {
        _logger.LogWarning(recEx, "Failed to generate recommendations, continuing without them");
    }
}
else
{
    _logger.LogInformation("Skipping recommendations generation - insights disabled for tenant {TenantId}", tenantId);
}
```

### 3. TenantService Updates

**File:** `src/AIQueryPlatform.Api/Services/TenantService.cs`

**New Method:**
```csharp
public async Task UpdateInsightsSettingAsync(Guid tenantId, bool enableInsights)
{
    // Updates Tenants.EnableInsights column
    // Invalidates cache
}
```

**Updated Queries:** All tenant queries now include `EnableInsights` column.

### 4. API Endpoints

#### Get Current Tenant Info

```http
GET /api/tenant/current
Headers:
  x-api-key: {apiKey}
  x-tenant-id: {tenantId}

Response:
{
  "tenantId": "guid",
  "name": "Acme Corp",
  "logoUrl": null,
  "themeColor": "#667eea",
  "isActive": true,
  "enableInsights": true  // ← NEW
}
```

#### Update Insights Setting (Admin)

```http
PATCH /api/tenant/insights
Headers:
  x-api-key: {apiKey}
  x-tenant-id: {tenantId}
Content-Type: application/json

Body:
{
  "enableInsights": false
}

Response:
{
  "message": "Insights setting updated successfully",
  "enableInsights": false,
  "note": "AI recommendations disabled (tokens saved)"
}
```

## Frontend Implementation

### 1. API Service

**File:** `widget/src/services/api.ts`

```typescript
async getTenantInfo(): Promise<{ enableInsights: boolean; name: string; themeColor?: string }> {
  try {
    const response = await fetch(`${this.config.apiBaseUrl}/api/tenant/current`, {
      method: 'GET',
      headers: {
        'x-api-key': this.config.apiKey,
        'x-tenant-id': this.config.tenantId,
      },
    });

    if (!response.ok) {
      return { enableInsights: true, name: 'Unknown' };
    }

    const data = await response.json();
    return {
      enableInsights: data.enableInsights ?? true,
      name: data.name,
      themeColor: data.themeColor,
    };
  } catch (error) {
    console.error('[APIService] Error fetching tenant info:', error);
    return { enableInsights: true, name: 'Unknown' };
  }
}
```

### 2. Modal Workspace

**File:** `widget/src/modal/ModalWorkspace.tsx`

```typescript
// Server-side insights setting - fetched from tenant configuration
const [insightsEnabled, setInsightsEnabled] = useState(true);

// Fetch tenant configuration on mount
useEffect(() => {
  const fetchTenantConfig = async () => {
    try {
      const tenantInfo = await apiService.getTenantInfo();
      setInsightsEnabled(tenantInfo.enableInsights);
      
      // If insights disabled, collapse panel immediately
      if (!tenantInfo.enableInsights) {
        setState(prev => ({ ...prev, rightPanelCollapsed: true }));
      }
    } catch (error) {
      console.error('[ModalWorkspace] Failed to fetch tenant config:', error);
    }
  };
  
  fetchTenantConfig();
}, [apiService]);

// Conditional rendering
{insightsEnabled && !state.rightPanelCollapsed && conversation && (
  <RightPanel
    conversation={conversation}
    messages={messages}
    onToggle={handleToggleRightPanel}
  />
)}
```

## Configuration Management

### View Current Settings

```sql
SELECT 
    TenantId,
    Name,
    EnableInsights,
    CASE 
        WHEN EnableInsights = 1 THEN 'Recommendations will be generated (tokens consumed)'
        ELSE 'Recommendations disabled (tokens saved)'
    END AS Status
FROM Tenants;
```

### Disable Insights for a Tenant

```sql
UPDATE Tenants
SET EnableInsights = 0,
    UpdatedAt = GETUTCDATE()
WHERE TenantId = 'YOUR-TENANT-ID-HERE';
```

### Enable Insights for a Tenant

```sql
UPDATE Tenants
SET EnableInsights = 1,
    UpdatedAt = GETUTCDATE()
WHERE TenantId = 'YOUR-TENANT-ID-HERE';
```

### Via API (Recommended)

```bash
# Disable insights
curl -X PATCH http://localhost:5000/api/tenant/insights \
  -H "x-api-key: your-api-key" \
  -H "x-tenant-id: your-tenant-id" \
  -H "Content-Type: application/json" \
  -d '{"enableInsights": false}'

# Enable insights
curl -X PATCH http://localhost:5000/api/tenant/insights \
  -H "x-api-key: your-api-key" \
  -H "x-tenant-id: your-tenant-id" \
  -H "Content-Type: application/json" \
  -d '{"enableInsights": true}'
```

## Testing

### 1. Run Database Migration

```powershell
sqlcmd -S localhost -d AIQueryPlatform -i database/add_enable_insights_column.sql
```

### 2. Rebuild API

```powershell
cd src/AIQueryPlatform.Api
dotnet build
dotnet run
```

### 3. Rebuild Widget

```powershell
cd widget
npm run build
```

### 4. Test Insights Enabled (Default)

1. Verify tenant setting:
   ```sql
   SELECT EnableInsights FROM Tenants WHERE TenantId = 'your-tenant-id';
   -- Should return 1
   ```

2. Send a query via widget
3. Open modal workspace
4. **Expected:** Right panel visible with recommendations
5. Check logs:
   ```
   [ConversationsController] Generating AI recommendations for query results (insights enabled for tenant)
   [RecommendationService] Generating recommendations for query...
   ```

### 5. Test Insights Disabled

1. Disable for tenant:
   ```sql
   UPDATE Tenants SET EnableInsights = 0 WHERE TenantId = 'your-tenant-id';
   ```

2. Restart API (to clear cache)
3. Send a query via widget
4. Open modal workspace
5. **Expected:** 
   - No right panel
   - Main workspace full width
6. Check logs:
   ```
   [ConversationsController] Skipping recommendations generation - insights disabled for tenant {TenantId}
   ```
7. **Verify:** No calls to `RecommendationService.GenerateRecommendationsAsync`

## Token Savings Calculation

### Example Scenario

**Query:** "Show top 10 customers by revenue"

#### With Insights Enabled:
- NL-to-SQL: ~250 tokens
- **Recommendations:** ~500 tokens
- **Total:** ~750 tokens

#### With Insights Disabled:
- NL-to-SQL: ~250 tokens
- Recommendations: **0 tokens** (skipped)
- **Total:** ~250 tokens

**Savings:** 66% token reduction per query!

### Monthly Savings

**Assumptions:**
- 1,000 queries/month
- 500 tokens/query for recommendations
- $0.002 per 1K tokens (GPT-4 pricing)

**With insights enabled:**
- 1,000 queries × 500 tokens = 500,000 tokens
- Cost: $1.00/month

**With insights disabled:**
- 0 tokens for recommendations
- **Savings: $1.00/month per tenant**

For 100 tenants: **$100/month savings**

## Use Cases

### Enable Insights For:
- ✅ Premium/Enterprise tier customers
- ✅ Business analysts who need AI guidance
- ✅ Users exploring data patterns
- ✅ Decision-makers requiring recommendations

### Disable Insights For:
- ✅ Free/Basic tier customers
- ✅ Cost-sensitive deployments
- ✅ Simple query interfaces
- ✅ Tenants with external BI tools
- ✅ Performance-critical scenarios

## Troubleshooting

### Issue: Insights still generating when disabled

**Check 1:** Verify database setting
```sql
SELECT EnableInsights FROM Tenants WHERE TenantId = 'your-tenant-id';
```

**Check 2:** Clear cache and restart API
```powershell
# Restart the API to clear memory cache
dotnet run
```

**Check 3:** Check logs
Look for: `"Skipping recommendations generation - insights disabled"`

### Issue: Frontend still showing insights panel

**Check 1:** Verify API response
```bash
curl http://localhost:5000/api/tenant/current \
  -H "x-api-key: your-key" \
  -H "x-tenant-id: your-id"
```

Should return: `"enableInsights": false`

**Check 2:** Rebuild widget
```powershell
cd widget
npm run build
# Clear browser cache: Ctrl+Shift+R
```

**Check 3:** Check browser console
```
[ModalWorkspace] Tenant insights setting: false
```

### Issue: API returns 500 error

**Check:** Column exists in database
```sql
SELECT * FROM sys.columns 
WHERE object_id = OBJECT_ID('Tenants') AND name = 'EnableInsights';
```

If missing, run the migration script.

## Security Considerations

- **Authorization:** Only admin users should be able to change `EnableInsights`
- **Validation:** Ensure `enableInsights` is boolean in API requests
- **Audit:** Log all changes to insights settings
- **Cache:** Invalidate tenant cache after updates

## Future Enhancements

1. **Per-Feature Pricing:** Link `EnableInsights` to subscription tiers
2. **Usage Limits:** Allow X recommendations/month for free tier
3. **A/B Testing:** Enable insights for 50% of users to measure impact
4. **Analytics:** Track token savings per tenant
5. **Admin UI:** Web interface to manage feature flags

## Related Files

- `database/add_enable_insights_column.sql` - Database migration
- `src/AIQueryPlatform.Api/Models/Tenant.cs` - Tenant model
- `src/AIQueryPlatform.Api/Controllers/ConversationsController.cs` - Conditional logic
- `src/AIQueryPlatform.Api/Services/TenantService.cs` - Data access
- `src/AIQueryPlatform.Api/Controllers/TenantController.cs` - API endpoints
- `widget/src/services/api.ts` - Frontend API service
- `widget/src/modal/ModalWorkspace.tsx` - UI conditional rendering

---

**Status:** ✅ IMPLEMENTED  
**Token Savings:** Up to 66% per query  
**Control:** Server-side (secure and centralized)  
**Backward Compatible:** ✅ Yes (defaults to enabled)
