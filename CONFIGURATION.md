# Configuration Guide

## Setting Up Local Development

To run this application locally, you need to configure your Azure OpenAI credentials and database connection.

### Option 1: Using appsettings.Local.json (Recommended)

1. Copy `appsettings.json` to `appsettings.Local.json`
2. Update the values in `appsettings.Local.json` with your actual credentials:

```json
{
  "OpenAI": {
    "Endpoint": "https://your-instance.openai.azure.com/",
    "ApiKey": "your-actual-api-key-here",
    "DeploymentName": "your-deployment-name"
  },
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

### Option 2: Using Environment Variables

Set the following environment variables:

```powershell
$env:OpenAI__Endpoint = "https://your-instance.openai.azure.com/"
$env:OpenAI__ApiKey = "your-actual-api-key-here"
$env:OpenAI__DeploymentName = "your-deployment-name"
$env:ConnectionStrings__DefaultConnection = "your-connection-string"
```

### Option 3: Using User Secrets (Development)

```powershell
cd src/AIQueryPlatform.Api
dotnet user-secrets set "OpenAI:ApiKey" "your-actual-api-key-here"
dotnet user-secrets set "OpenAI:Endpoint" "https://your-instance.openai.azure.com/"
dotnet user-secrets set "OpenAI:DeploymentName" "your-deployment-name"
```

## Important Security Notes

- **NEVER** commit actual API keys or secrets to version control
- `appsettings.Local.json`, `appsettings.Development.json`, and `appsettings.Production.json` are in `.gitignore` and should not be committed
- Use Azure Key Vault or environment variables for production deployments
- The `appsettings.json` file should only contain placeholder values

## Running the Application

After configuring your secrets:

```powershell
cd src/AIQueryPlatform.Api
dotnet run
```

The API will be available at `https://localhost:7000` (or as configured).
