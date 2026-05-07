# Troubleshooting: "Failed to generate SQL query" Error

## Common Causes and Solutions

### 1. Database Schema Not Updated

**Problem:** The Tenants table doesn't have the new `DatabaseType` and `DatabaseSettings` columns.

**Solution:**
```powershell
# Run the database setup script
sqlcmd -S MWP336\SQLEXPRESS -E -i database/setup.sql
```

Or execute [setup.sql](database/setup.sql) manually in SQL Server Management Studio.

**Verify:**
```powershell
# Run verification script
sqlcmd -S MWP336\SQLEXPRESS -E -i database/verify_schema.sql
```

### 2. Missing OpenAI API Key

**Problem:** The Azure OpenAI API key is not configured or is using placeholder values.

**Solution:** Verify your [appsettings.Local.json](src/AIQueryPlatform.Api/appsettings.Local.json) has real credentials:

```json
{
  "OpenAI": {
    "Endpoint": "https://your-instance.openai.azure.com/",
    "ApiKey": "your-actual-api-key-here",
    "DeploymentName": "your-deployment-name"
  }
}
```

**Alternative:** Use environment variables:
```powershell
$env:OpenAI__ApiKey = "your-actual-api-key"
$env:OpenAI__Endpoint = "https://your-instance.openai.azure.com/"
$env:OpenAI__DeploymentName = "your-deployment"
```

### 3. NuGet Packages Not Restored

**Problem:** New database driver packages weren't restored.

**Solution:**
```powershell
cd src/AIQueryPlatform.Api
dotnet restore --force
dotnet build
```

### 4. Service Registration Issues

**Problem:** Database executors or prompt builders not registered correctly.

**Check:** Look for errors in the application logs:
```powershell
Get-Content src/AIQueryPlatform.Api/logs/*.log -Tail 50
```

### 5. Database Connection Issues

**Problem:** Cannot connect to tenant database.

**Solution:**
```powershell
# Test connection to AIQueryPlatform database
sqlcmd -S MWP336\SQLEXPRESS -E -Q "SELECT @@VERSION"

# Test connection to DemoTenantDB
sqlcmd -S MWP336\SQLEXPRESS -E -d DemoTenantDB -Q "SELECT COUNT(*) FROM Customers"
```

## Step-by-Step Resolution

### Step 1: Update Database Schema
```powershell
# Navigate to project root
cd c:\Users\siddharth.jain\Desktop\AIQueryPlatform

# Run setup script
sqlcmd -S MWP336\SQLEXPRESS -E -i database\setup.sql

# Verify schema
sqlcmd -S MWP336\SQLEXPRESS -E -i database\verify_schema.sql
```

### Step 2: Verify Configuration
```powershell
# Check if appsettings.Local.json exists
Test-Path src\AIQueryPlatform.Api\appsettings.Local.json

# Verify it has your API key (should show your actual key, not placeholder)
Get-Content src\AIQueryPlatform.Api\appsettings.Local.json | Select-String "ApiKey"
```

### Step 3: Rebuild Application
```powershell
cd src\AIQueryPlatform.Api
dotnet clean
dotnet restore --force
dotnet build
```

### Step 4: Run Application with Verbose Logging
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj
```

Watch the console for any errors during startup.

### Step 5: Test API
Open another terminal and test:
```powershell
# Test tenant endpoint
Invoke-RestMethod -Uri "http://localhost:7000/api/tenant/info" `
  -Headers @{"x-api-key"="demo_api_key_12345"} `
  -Method Get

# Test query endpoint
$body = @{
    query = "show me top 5 customers"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7000/api/query/execute" `
  -Headers @{"x-api-key"="demo_api_key_12345"; "Content-Type"="application/json"} `
  -Method Post `
  -Body $body
```

## Specific Error Messages

### "Azure.RequestFailedException: Access denied"
- Your OpenAI API key is invalid or expired
- Generate a new key at Azure Portal
- Update appsettings.Local.json

### "No tenant context available"
- API key header missing or invalid
- Use: `-Headers @{"x-api-key"="demo_api_key_12345"}`

### "Database error: Invalid object name 'Tenants'"
- Database schema not created
- Run database/setup.sql script

### "The type or namespace name 'MySql' could not be found"
- NuGet packages not restored
- Run: `dotnet restore --force`

### "Unable to resolve service for type 'DatabasePromptBuilderFactory'"
- Services not registered in Program.cs
- This should be fixed in latest code

## Check Application Logs

```powershell
# View recent logs
Get-Content src/AIQueryPlatform.Api/logs/*.log -Tail 100 | Select-String -Pattern "error|exception" -Context 2,2
```

## Validate Multi-Database Setup

Run this SQL to check tenant configuration:
```sql
USE AIQueryPlatform;
SELECT 
    Name,
    DatabaseType,
    CASE DatabaseType
        WHEN 0 THEN 'SQL Server'
        WHEN 1 THEN 'MySQL'  
        WHEN 2 THEN 'PostgreSQL'
        WHEN 3 THEN 'Excel'
        WHEN 4 THEN 'SQLite'
    END AS TypeName,
    LEFT(ApiKey, 20) + '...' AS ApiKeyPreview,
    IsActive
FROM Tenants;
```

## Still Having Issues?

1. **Check the exact error message** in logs:
   - Location: `src/AIQueryPlatform.Api/logs/aiquery-{date}.log`
   - Look for stack traces

2. **Verify environment**:
   ```powershell
   dotnet --version  # Should be 8.0.x
   sqlcmd -?  # Should show SQL Server tools
   ```

3. **Test OpenAI connection separately**:
   ```csharp
   // Test in a simple console app
   var client = new OpenAIClient(
       new Uri("your-endpoint"),
       new AzureKeyCredential("your-key"));
   ```

4. **Reset to clean state**:
   ```powershell
   # Drop and recreate databases
   sqlcmd -S MWP336\SQLEXPRESS -E -Q "DROP DATABASE AIQueryPlatform"
   sqlcmd -S MWP336\SQLEXPRESS -E -Q "DROP DATABASE DemoTenantDB"
   
   # Run setup again
   sqlcmd -S MWP336\SQLEXPRESS -E -i database\setup.sql
   ```

## Quick Fix Checklist

- [ ] Database schema updated (DatabaseType column exists)
- [ ] appsettings.Local.json has real API key (not placeholder)
- [ ] NuGet packages restored (`dotnet restore`)
- [ ] Application builds without errors (`dotnet build`)
- [ ] SQL Server running and accessible
- [ ] Tenant exists with valid API key
- [ ] Application runs without startup errors
- [ ] API responds to health check

## Need More Help?

Check the following documentation:
- [MULTI_DATABASE_SUPPORT.md](MULTI_DATABASE_SUPPORT.md) - Multi-database feature guide
- [CONFIGURATION.md](CONFIGURATION.md) - Configuration setup guide
- [DEVELOPMENT.md](DEVELOPMENT.md) - Development guide
