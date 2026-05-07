# Quick Start Guide

## 🚀 Getting Started in 5 Minutes

### Step 1: Install Prerequisites
- Install .NET 8 SDK from https://dotnet.microsoft.com/download
- Install SQL Server (Express is fine)
- Get OpenAI API key or Azure OpenAI access

### Step 2: Setup Database
```bash
# Open SQL Server Management Studio or Azure Data Studio
# Run the script: database/setup.sql
# This creates sample tenant and data
```

### Step 3: Configure API
Edit `src/AIQueryPlatform.Api/appsettings.json`:
```json
{
  "OpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "DeploymentName": "gpt-4"
  }
}
```

### Step 4: Run the API
```bash
cd src/AIQueryPlatform.Api
dotnet run
```

API will start at: https://localhost:7001

### Step 5: Test with Frontend
1. Open `frontend/index.html` in your browser
2. API Key is pre-filled: `demo_api_key_12345`
3. Try example query: "Show me top 10 customers by revenue"

## 🎯 Example Queries to Try

- "Show me top 10 customers by revenue"
- "What are the total sales by country?"
- "List all products with low stock"
- "Show me orders from last 30 days"
- "Generate a sales summary report"

## 📊 Testing with Swagger

1. Navigate to: https://localhost:7001/swagger
2. Click "Authorize"
3. Enter API Key: `demo_api_key_12345`
4. Try the `/api/query/execute` endpoint

## 🐳 Using Docker (Alternative)

```bash
# Create .env file from example
cp .env.example .env

# Edit .env with your OpenAI credentials

# Start services
docker-compose up -d

# API will be at http://localhost:8080
```

## 🔧 Troubleshooting

**Problem: Can't connect to database**
```
Solution: Update connection string in appsettings.json
Verify SQL Server is running
```

**Problem: OpenAI errors**
```
Solution: Verify endpoint and API key
Check deployment name matches your Azure OpenAI model
```

**Problem: "Invalid API key"**
```
Solution: Use: demo_api_key_12345
Or check Tenants table for valid keys
```

## 📚 Next Steps

- Read [README.md](README.md) for full documentation
- Check [DEVELOPMENT.md](DEVELOPMENT.md) for development guide
- See [DEPLOYMENT.md](DEPLOYMENT.md) for production deployment

## 🆘 Need Help?

- Check logs in `logs/` directory
- Review Swagger documentation at `/swagger`
- Open an issue on GitHub

## ✅ Quick Test

```bash
# Test health endpoint
curl https://localhost:7001/health

# Test query (skip SSL verification for local dev)
curl -k -X POST https://localhost:7001/api/query/execute \
  -H "Content-Type: application/json" \
  -H "x-api-key: demo_api_key_12345" \
  -d '{"query":"Show me top 5 customers"}'
```

Happy querying! 🎉
