# AI Query Platform - Deployment Guide

## Prerequisites

### Required Software
- .NET 8 SDK
- SQL Server 2019 or later (or Azure SQL Database)
- IIS (for Windows) or Nginx/Apache (for Linux)
- Azure OpenAI or OpenAI API account

### Azure Services (Optional but Recommended)
- Azure App Service (for hosting API)
- Azure SQL Database (for data storage)
- Azure OpenAI Service (for AI capabilities)
- Azure Application Insights (for monitoring)

## Local Development Setup

### 1. Clone and Restore
```bash
cd AIQueryPlatform
dotnet restore
```

### 2. Database Setup
```bash
# Run the setup script in SQL Server Management Studio
# Or use sqlcmd:
sqlcmd -S localhost -i database/setup.sql
```

### 3. Configure Application
Edit `appsettings.Development.json`:

```json
{
  "OpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "DeploymentName": "gpt-4",
    "MaxTokens": 1000,
    "Temperature": 0.0
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AIQueryPlatform;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 4. Run Locally
```bash
cd src/AIQueryPlatform.Api
dotnet run
```

API will be available at: `https://localhost:7001`

### 5. Test with Frontend
Open `frontend/index.html` in a web browser.

## Production Deployment

### Option 1: Azure App Service

#### Step 1: Publish the Application
```bash
cd src/AIQueryPlatform.Api
dotnet publish -c Release -o ./publish
```

#### Step 2: Create Azure Resources
```bash
# Login to Azure
az login

# Create resource group
az group create --name AIQueryPlatform-RG --location eastus

# Create App Service Plan
az appservice plan create \
  --name AIQueryPlatform-Plan \
  --resource-group AIQueryPlatform-RG \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name aiquery-api \
  --resource-group AIQueryPlatform-RG \
  --plan AIQueryPlatform-Plan \
  --runtime "DOTNET|8.0"

# Create SQL Database
az sql server create \
  --name aiquery-sql \
  --resource-group AIQueryPlatform-RG \
  --location eastus \
  --admin-user sqladmin \
  --admin-password "YourPassword123!"

az sql db create \
  --name AIQueryPlatform \
  --server aiquery-sql \
  --resource-group AIQueryPlatform-RG \
  --service-objective S0
```

#### Step 3: Configure Application Settings
```bash
# Set connection string
az webapp config connection-string set \
  --name aiquery-api \
  --resource-group AIQueryPlatform-RG \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:aiquery-sql.database.windows.net,1433;Database=AIQueryPlatform;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;TrustServerCertificate=False;"

# Set OpenAI configuration
az webapp config appsettings set \
  --name aiquery-api \
  --resource-group AIQueryPlatform-RG \
  --settings \
    OpenAI__Endpoint="https://YOUR-RESOURCE.openai.azure.com/" \
    OpenAI__ApiKey="YOUR-API-KEY" \
    OpenAI__DeploymentName="gpt-4"
```

#### Step 4: Deploy
```bash
# Deploy using ZIP
cd publish
zip -r ../app.zip .
cd ..

az webapp deployment source config-zip \
  --name aiquery-api \
  --resource-group AIQueryPlatform-RG \
  --src app.zip
```

### Option 2: Docker Container

#### Create Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj", "src/AIQueryPlatform.Api/"]
RUN dotnet restore "src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj"
COPY . .
WORKDIR "/src/src/AIQueryPlatform.Api"
RUN dotnet build "AIQueryPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIQueryPlatform.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIQueryPlatform.Api.dll"]
```

#### Build and Run
```bash
# Build image
docker build -t aiquery-platform .

# Run container
docker run -d -p 8080:80 \
  -e OpenAI__Endpoint="YOUR-ENDPOINT" \
  -e OpenAI__ApiKey="YOUR-KEY" \
  -e ConnectionStrings__DefaultConnection="YOUR-CONNECTION-STRING" \
  aiquery-platform
```

## Database Migration

### Migrate Existing Tenant Data
```sql
-- Add new tenant
INSERT INTO AIQueryPlatform.dbo.Tenants (TenantId, Name, ApiKey, ConnectionString, IsActive)
VALUES (
  NEWID(),
  'New Tenant',
  'new_tenant_api_key',
  'Server=...;Database=NewTenantDB;...',
  1
);
```

## Security Hardening

### 1. Enable HTTPS
Ensure SSL/TLS certificates are properly configured.

### 2. API Key Rotation
```sql
-- Update tenant API key
UPDATE Tenants 
SET ApiKey = 'new_secure_api_key_' + CONVERT(NVARCHAR(36), NEWID()),
    UpdatedAt = GETUTCDATE()
WHERE TenantId = 'YOUR-TENANT-ID';
```

### 3. Configure CORS
Update `Program.cs` to restrict origins:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 4. Rate Limiting
Adjust limits in `appsettings.json`:
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 30,
    "RequestsPerHour": 500
  }
}
```

## Monitoring and Logging

### Application Insights (Azure)
```bash
# Enable Application Insights
az monitor app-insights component create \
  --app aiquery-insights \
  --location eastus \
  --resource-group AIQueryPlatform-RG

# Get instrumentation key
az monitor app-insights component show \
  --app aiquery-insights \
  --resource-group AIQueryPlatform-RG \
  --query instrumentationKey
```

Add to `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR-KEY"
  }
}
```

### Log Files
Logs are written to `logs/` directory by default.
Configure retention in `Program.cs` (Serilog).

## Performance Optimization

### 1. Enable Response Caching
Add to `Program.cs`:
```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### 2. Database Connection Pooling
Ensure connection strings include:
```
...;Min Pool Size=5;Max Pool Size=100;
```

### 3. Schema Caching
Schema is cached for 1 hour by default. Adjust in `SchemaService.cs`.

## Backup and Recovery

### Database Backup
```bash
# Azure SQL
az sql db export \
  --name AIQueryPlatform \
  --server aiquery-sql \
  --resource-group AIQueryPlatform-RG \
  --admin-user sqladmin \
  --admin-password "YourPassword123!" \
  --storage-key-type StorageAccessKey \
  --storage-key "YOUR-STORAGE-KEY" \
  --storage-uri "https://youraccount.blob.core.windows.net/backups/backup.bacpac"
```

### Application Backup
Backup configuration files and custom code regularly.

## Troubleshooting

### Common Issues

**Issue: "Invalid API key"**
- Verify API key in request header matches tenant record
- Check tenant IsActive status

**Issue: "Failed to generate SQL query"**
- Verify OpenAI endpoint and API key
- Check deployment name matches your Azure OpenAI deployment
- Review logs for detailed error messages

**Issue: "Database connection failed"**
- Verify connection string format
- Check firewall rules (Azure SQL)
- Ensure SQL Server is running

**Issue: "Rate limit exceeded"**
- Adjust rate limits in configuration
- Implement tenant-specific limits

## Support

For issues and questions:
- Check logs in `logs/` directory
- Review Application Insights (if configured)
- Contact support team

## License
MIT License - See LICENSE file
