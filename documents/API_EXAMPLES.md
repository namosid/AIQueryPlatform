# AI Query Platform - API Examples

## Authentication

All API requests require authentication using API key in header:

```http
x-api-key: demo_api_key_12345
```

## Base URL

```
Local: https://localhost:7001
Production: https://your-domain.com
```

## Endpoints

### 1. Execute Query (Streaming)

Stream query results in real-time using NDJSON format.

**Endpoint:** `POST /api/query/execute-stream`

**Request:**
```bash
curl -X POST https://localhost:7001/api/query/execute-stream \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{
    "query": "Show me top 10 customers by revenue"
  }' \
  --no-buffer
```

**Response:** (NDJSON stream)
```json
{"type":"log","message":"Processing your query...","timestamp":"2026-05-05T10:30:00Z"}
{"type":"log","message":"Loading database schema...","timestamp":"2026-05-05T10:30:01Z"}
{"type":"sql_generated","sql":"SELECT TOP 10 CustomerName, TotalRevenue FROM Customers ORDER BY TotalRevenue DESC","timestamp":"2026-05-05T10:30:02Z"}
{"type":"execution_progress","message":"Executing query against database...","timestamp":"2026-05-05T10:30:03Z"}
{"type":"final_result","data":{"columns":["CustomerName","TotalRevenue"],"rows":[...],"rowCount":10},"visualizationType":"table","timestamp":"2026-05-05T10:30:04Z"}
```

---

### 2. Execute Query (Standard)

Execute query and get complete response at once.

**Endpoint:** `POST /api/query/execute`

**Request:**
```bash
curl -X POST https://localhost:7001/api/query/execute \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{
    "query": "What are the total sales by country?"
  }'
```

**Response:**
```json
{
  "success": true,
  "generatedSql": "SELECT TOP 100 Country, SUM(TotalRevenue) as TotalSales FROM Customers GROUP BY Country ORDER BY TotalSales DESC",
  "result": {
    "columns": ["Country", "TotalSales"],
    "rows": [
      {"Country": "UK", "TotalSales": 225000.00},
      {"Country": "UAE", "TotalSales": 205000.00},
      {"Country": "Singapore", "TotalSales": 215000.00}
    ],
    "rowCount": 8
  },
  "visualizationType": "chart",
  "executionTimeMs": 1243
}
```

---

### 3. Generate PDF Report

Generate and download a PDF report.

**Endpoint:** `POST /api/query/generate-report`

**Request:**
```bash
curl -X POST https://localhost:7001/api/query/generate-report \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{
    "query": "Generate sales summary report",
    "reportTitle": "Q1 Sales Report"
  }' \
  -o report.pdf
```

**Response:** Binary PDF file

---

### 4. Convert to Chart Data

Convert query result to chart-friendly format.

**Endpoint:** `POST /api/query/to-chart`

**Request:**
```bash
curl -X POST https://localhost:7001/api/query/to-chart \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{
    "query": "Show me product sales by category"
  }'
```

**Response:**
```json
{
  "labels": ["Software", "Cloud Services", "Services"],
  "values": [15000, 22000, 18500],
  "chartType": "bar"
}
```

---

### 5. Get Current Tenant Info

Get information about the authenticated tenant.

**Endpoint:** `GET /api/tenant/current`

**Request:**
```bash
curl -X GET https://localhost:7001/api/tenant/current \
  -H "x-api-key: demo_api_key_12345"
```

**Response:**
```json
{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "name": "Demo Tenant",
  "logoUrl": "https://example.com/logo.png",
  "themeColor": "#0066CC",
  "isActive": true
}
```

---

### 6. Invalidate Schema Cache

Clear cached database schema for current tenant.

**Endpoint:** `POST /api/tenant/invalidate-cache`

**Request:**
```bash
curl -X POST https://localhost:7001/api/tenant/invalidate-cache \
  -H "x-api-key: demo_api_key_12345"
```

**Response:**
```json
{
  "message": "Schema cache invalidated successfully"
}
```

---

### 7. Health Check

Check API health status.

**Endpoint:** `GET /health`

**Request:**
```bash
curl -X GET https://localhost:7001/health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-05-05T10:30:00Z",
  "version": "1.0.0"
}
```

---

## Error Responses

### 400 Bad Request
```json
{
  "error": "Query cannot be empty",
  "timestamp": "2026-05-05T10:30:00Z"
}
```

### 401 Unauthorized
```json
{
  "error": "Invalid or inactive API key",
  "timestamp": "2026-05-05T10:30:00Z"
}
```

### 403 Forbidden
```json
{
  "error": "SQL validation failed: Dangerous SQL keyword detected: DELETE",
  "timestamp": "2026-05-05T10:30:00Z"
}
```

### 429 Too Many Requests
```json
{
  "error": "Rate limit exceeded",
  "limit": 60,
  "window": "1 minute",
  "retryAfter": 60,
  "timestamp": "2026-05-05T10:30:00Z"
}
```

### 500 Internal Server Error
```json
{
  "error": "Internal server error",
  "details": "Failed to execute query",
  "timestamp": "2026-05-05T10:30:00Z"
}
```

---

## JavaScript SDK Examples

### Initialize SDK
```javascript
const sdk = new AIQueryUI({
  apiUrl: 'https://localhost:7001',
  apiKey: 'demo_api_key_12345',
  resultContainerId: 'result',
  logContainerId: 'logs'
});

sdk.init();
```

### Execute Query with Streaming
```javascript
await sdk.sendQuery('Show me top 10 customers');
```

### Execute Query (Non-streaming)
```javascript
await sdk.sendQuerySync('Show me top 10 customers');
```

### Generate PDF Report
```javascript
await sdk.generateReport('Sales summary report', 'Q1 Report');
```

---

## Python Examples

### Execute Query
```python
import requests

url = "https://localhost:7001/api/query/execute"
headers = {
    "Content-Type": "application/json",
    "x-api-key": "demo_api_key_12345"
}
payload = {
    "query": "Show me top 10 customers by revenue"
}

response = requests.post(url, json=payload, headers=headers, verify=False)
result = response.json()

if result['success']:
    for row in result['result']['rows']:
        print(row)
```

### Stream Query Results
```python
import requests
import json

url = "https://localhost:7001/api/query/execute-stream"
headers = {
    "Content-Type": "application/json",
    "x-api-key": "demo_api_key_12345"
}
payload = {
    "query": "Show me top 10 customers"
}

response = requests.post(url, json=payload, headers=headers, stream=True, verify=False)

for line in response.iter_lines():
    if line:
        event = json.loads(line)
        print(f"[{event['type']}] {event.get('message', '')}")
```

---

## PowerShell Examples

### Execute Query
```powershell
$headers = @{
    "Content-Type" = "application/json"
    "x-api-key" = "demo_api_key_12345"
}

$body = @{
    query = "Show me top 10 customers by revenue"
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "https://localhost:7001/api/query/execute" `
    -Method Post `
    -Headers $headers `
    -Body $body `
    -SkipCertificateCheck

$response.result.rows | Format-Table
```

### Download PDF Report
```powershell
$headers = @{
    "Content-Type" = "application/json"
    "x-api-key" = "demo_api_key_12345"
}

$body = @{
    query = "Generate sales report"
    reportTitle = "Sales Report"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:7001/api/query/generate-report" `
    -Method Post `
    -Headers $headers `
    -Body $body `
    -OutFile "report.pdf" `
    -SkipCertificateCheck
```

---

## Rate Limiting

Default limits per tenant:
- **60 requests per minute**
- **1000 requests per hour**

When rate limit is exceeded, the API returns **429 Too Many Requests** with a `Retry-After` header indicating when to retry.

---

## Best Practices

1. **Use streaming for real-time updates** - Better user experience
2. **Cache results when appropriate** - Reduce API calls
3. **Handle rate limits gracefully** - Implement exponential backoff
4. **Validate input before sending** - Faster error detection
5. **Use appropriate visualization** - Let the Intelligence Layer decide
6. **Monitor query performance** - Check `executionTimeMs` in responses
7. **Invalidate schema cache** - When database structure changes

---

For more examples and documentation, visit the [GitHub repository](https://github.com/yourorg/aiquery-platform).
