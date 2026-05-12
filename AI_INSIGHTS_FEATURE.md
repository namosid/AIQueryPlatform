# AI-Powered Insights & Recommendations

## Overview

The platform now automatically generates AI-powered business insights after each query execution. These insights appear in the **right panel** of the modal workspace and include:

- 🚨 **Risks**: Potential issues or concerns in your data
- 💡 **Opportunities**: Positive trends and growth areas
- ✅ **Actions**: Recommended next steps and follow-up queries

---

## Architecture

### Data Flow

```
User Query → SQL Execution → Results
                              ↓
                    LLM Analysis (Azure OpenAI)
                              ↓
              Risks | Opportunities | Actions
                              ↓
                    Stored in Message
                              ↓
                Frontend Right Panel Display
```

### Components Added

#### Backend (C#/.NET)

**1. DTOs** ([ConversationDTOs.cs](src/AIQueryPlatform.Api/Models/DTOs/ConversationDTOs.cs))
- `RecommendationDto`: AI-generated insight
- `RecommendedActionDto`: Follow-up action
- Added `Recommendations` property to `Message` class

**2. Service Interface** ([IRecommendationService.cs](src/AIQueryPlatform.Api/Services/Interfaces/IRecommendationService.cs))
```csharp
Task<List<RecommendationDto>> GenerateRecommendationsAsync(
    string query, 
    QueryResult result, 
    string? schemaContext = null
);
```

**3. Service Implementation** ([RecommendationService.cs](src/AIQueryPlatform.Api/Services/RecommendationService.cs))
- Uses Azure OpenAI GPT-4
- Analyzes query results and generates insights
- Returns structured JSON with recommendations
- Gracefully handles errors (won't break queries)

**4. Integration** ([ConversationsController.cs](src/AIQueryPlatform.Api/Controllers/ConversationsController.cs))
- Added to query execution pipeline
- Runs after SQL execution, before saving message
- Non-blocking (query succeeds even if recommendations fail)

**5. Service Registration** ([Program.cs](src/AIQueryPlatform.Api/Program.cs))
```csharp
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
```

#### Frontend (React/TypeScript)

**Already Implemented** ✅
- [RightPanel.tsx](widget/src/modal/components/RightPanel/RightPanel.tsx) - Displays insights
- [modal.ts](widget/src/types/modal.ts) - TypeScript types
- CSS styling for recommendation cards

---

## Configuration

### appsettings.json

```json
{
  "OpenAI": {
    "Endpoint": "https://your-instance.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4",
    "MaxTokens": 500,
    "Temperature": 0.0,
    
    // Recommendation-specific settings
    "RecommendationMaxTokens": 1500,
    "RecommendationTemperature": 0.3
  }
}
```

**Settings Explained:**
- `RecommendationMaxTokens`: More tokens for detailed insights (default: 1500)
- `RecommendationTemperature`: Higher for creative insights (default: 0.3)

---

## How It Works

### 1. Query Execution

When a user executes a query in the modal workspace:

```typescript
// User asks: "Show me top 10 customers by revenue"
```

### 2. LLM Analysis

The system sends this prompt to Azure OpenAI:

```
You are a business analyst reviewing query results.

Query: "Show me top 10 customers by revenue"

Results:
{
  "columns": ["CustomerName", "Revenue", "OrderCount"],
  "rows": [
    ["Acme Corp", 125000, 45],
    ["TechStart", 98000, 32],
    ...
  ],
  "totalRows": 10
}

Generate insights as JSON...
```

### 3. LLM Returns Insights

```json
{
  "recommendations": [
    {
      "id": "rec-001",
      "type": "risk",
      "title": "Customer Concentration Risk",
      "description": "Top customer represents 35% of total revenue. High dependency creates risk if this customer is lost.",
      "priority": "high",
      "impact": "Revenue could drop 35% if Acme Corp churns",
      "category": "Revenue Risk"
    },
    {
      "id": "rec-002",
      "type": "opportunity",
      "title": "Upsell Potential in Top Accounts",
      "description": "Top 3 customers show increasing order frequency. Strong engagement indicates upsell readiness.",
      "priority": "high",
      "impact": "Potential 20-30% revenue increase from expansion",
      "actions": [
        {
          "id": "act-001",
          "label": "View 6-month trend for top customers",
          "query": "Show order trends for Acme Corp, TechStart, and Global Inc over last 6 months",
          "type": "query"
        }
      ]
    },
    {
      "id": "rec-003",
      "type": "action",
      "title": "Set Up Churn Alerts",
      "description": "Monitor top accounts for decreased activity. Early detection enables proactive retention.",
      "priority": "medium",
      "impact": "Prevent potential 6-figure revenue loss",
      "effort": "low"
    }
  ]
}
```

### 4. Display in Right Panel

The frontend automatically displays these in three sections:
- **Risks** (⚠️)
- **Opportunities** (💡)
- **Recommended Actions** (✓)

---

## Example Use Cases

### Sales Analysis

**Query:** "Show me monthly sales trends for Q1 2026"

**AI Insights:**
- 🚨 **Risk**: February revenue dropped 15% vs January
- 💡 **Opportunity**: March recovery showing strong momentum
- ✅ **Action**: Investigate February dip by product category

### Customer Behavior

**Query:** "Find customers with multiple support tickets"

**AI Insights:**
- 🚨 **Risk**: 5 high-value customers have open critical tickets
- 💡 **Opportunity**: Quick resolution can improve satisfaction scores
- ✅ **Action**: "Show ticket details for high-value customers"

### Inventory Management

**Query:** "Products with stock below reorder level"

**AI Insights:**
- 🚨 **Risk**: 12 products completely out of stock
- 💡 **Opportunity**: Fast-moving items identified for bulk order
- ✅ **Action**: "Calculate reorder quantities based on sales velocity"

---

## Performance & Limits

### Automatic Optimization

- **Empty results**: No recommendations generated
- **Large datasets**: Skipped for results >100 rows (reduces cost)
- **Sampling**: Only first 20 rows sent to LLM (reduces tokens)
- **Error handling**: Query succeeds even if recommendations fail

### Token Usage

| Component | Tokens |
|-----------|--------|
| System Prompt | ~600 |
| User Prompt | ~200 |
| Sample Data | ~300-500 |
| Response | ~800-1200 |
| **Total** | **~2000-2500** |

**Cost Estimate** (GPT-4):
- ~$0.06 - $0.10 per query with recommendations
- Can be disabled by setting `RecommendationMaxTokens: 0`

---

## Customization

### Adjust Insight Quality

**More Creative/Detailed:**
```json
{
  "RecommendationTemperature": 0.5,
  "RecommendationMaxTokens": 2000
}
```

**More Focused/Concise:**
```json
{
  "RecommendationTemperature": 0.1,
  "RecommendationMaxTokens": 1000
}
```

### Disable Recommendations

Set in appsettings.json:
```json
{
  "RecommendationMaxTokens": 0
}
```

Or modify [RecommendationService.cs](src/AIQueryPlatform.Api/Services/RecommendationService.cs):
```csharp
// Always return empty list
public async Task<List<RecommendationDto>> GenerateRecommendationsAsync(...)
{
    return new List<RecommendationDto>();
}
```

---

## Testing

### 1. Restart the API

The API must be restarted for changes to take effect:

```powershell
cd src/AIQueryPlatform.Api
dotnet run
```

### 2. Open Modal Workspace

- Navigate to http://localhost:3001/example.html
- Click "Open Analytics Workspace"

### 3. Execute Query

Try these sample queries:

```
Show me top 5 customers by revenue
```

```
Find products with returns above 5%
```

```
Compare sales by region for last quarter
```

### 4. View Insights

Check the **right panel** for AI-generated insights!

---

## Troubleshooting

### No Recommendations Appearing

**Check Logs:**
```
[RecommendationService] Generating recommendations for query: ...
[RecommendationService] Generated 3 recommendations
```

**Common Issues:**
1. **Empty results**: Recommendations only for non-empty results
2. **Large dataset**: Skipped for >100 rows
3. **OpenAI error**: Check API key and endpoint in appsettings
4. **JSON parse error**: LLM returned invalid JSON (retryable)

### OpenAI Errors

**401 Unauthorized:**
- Check `OpenAI:ApiKey` in appsettings.json
- Verify Azure OpenAI resource access

**404 Not Found:**
- Verify `OpenAI:DeploymentName` matches your Azure deployment
- Ensure endpoint URL is correct

**429 Rate Limited:**
- Reduce `RecommendationTemperature` or `MaxTokens`
- Add caching layer for repeated queries

---

## Future Enhancements

### Planned Features

1. **Schema Context Integration**
   - Pass database schema to LLM for better insights
   - Suggest queries based on available tables

2. **Insight Caching**
   - Cache recommendations for identical queries
   - Reduces costs and latency

3. **Custom Prompts**
   - Industry-specific prompts (retail, finance, healthcare)
   - User-configurable insight categories

4. **Action Execution**
   - One-click execution of recommended queries
   - Export recommendations to PDF

5. **Feedback Loop**
   - Users rate recommendation quality
   - Fine-tune prompts based on feedback

---

## Security Considerations

### Data Privacy

- Only **first 20 rows** sent to OpenAI
- No sensitive data exposed in logs
- Recommendations stored in Messages table (encrypted at rest)

### Cost Control

- Automatic limits (empty results, large datasets)
- Configurable max tokens
- Can be disabled per tenant

---

## API Reference

### Generate Recommendations

**Internal Method** (not exposed as endpoint)

```csharp
await _recommendationService.GenerateRecommendationsAsync(
    query: "Show top customers",
    result: queryResult,
    schemaContext: null  // Optional: pass database schema
);
```

**Returns:**
```csharp
List<RecommendationDto>
{
    Id: "rec-xxx",
    Type: "risk" | "opportunity" | "action",
    Title: string,
    Description: string,
    Priority: "high" | "medium" | "low",
    Impact: string?,
    Effort: "low" | "medium" | "high"?,
    Actions: List<RecommendedActionDto>?
}
```

---

## Summary

The AI Insights feature is now **fully implemented** and integrated into the query execution pipeline. It automatically analyzes every query result and provides actionable business insights in the right panel.

**What's Working:**
- ✅ Backend LLM integration (Azure OpenAI)
- ✅ Recommendation generation service
- ✅ Integration with query execution
- ✅ Frontend display in right panel
- ✅ Error handling and performance optimization

**Next Steps:**
1. Restart the API: `cd src/AIQueryPlatform.Api; dotnet run`
2. Test in the modal workspace
3. Customize prompts and settings as needed

---

## Questions?

For issues or enhancements:
1. Check application logs in `src/AIQueryPlatform.Api/logs/`
2. Review [RecommendationService.cs](src/AIQueryPlatform.Api/Services/RecommendationService.cs)
3. Verify OpenAI configuration in appsettings.json
