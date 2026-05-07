# Quick Setup Script for Multi-Database Support
# Run this to set up the environment after multi-database changes

Write-Host "=== AI Query Platform - Multi-Database Setup ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = "c:\Users\siddharth.jain\Desktop\AIQueryPlatform"
Set-Location $projectRoot

# Step 1: Update Database Schema
Write-Host "Step 1: Updating database schema..." -ForegroundColor Yellow
try {
    sqlcmd -S MWP336\SQLEXPRESS -E -i database\setup.sql
    Write-Host "✓ Database schema updated successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Database update failed: $_" -ForegroundColor Red
    Write-Host "  Please run manually: sqlcmd -S MWP336\SQLEXPRESS -E -i database\setup.sql" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Verify Schema
Write-Host "Step 2: Verifying database schema..." -ForegroundColor Yellow
try {
    $result = sqlcmd -S MWP336\SQLEXPRESS -E -Q "USE AIQueryPlatform; SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Tenants') AND name IN ('DatabaseType', 'DatabaseSettings')" -h -1
    if ($result -eq "2") {
        Write-Host "✓ Database schema verified - new columns exist" -ForegroundColor Green
    } else {
        Write-Host "✗ Database schema incomplete - missing columns" -ForegroundColor Red
    }
} catch {
    Write-Host "! Could not verify schema: $_" -ForegroundColor Yellow
}

Write-Host ""

# Step 3: Check Configuration
Write-Host "Step 3: Checking configuration..." -ForegroundColor Yellow
$localConfigPath = "src\AIQueryPlatform.Api\appsettings.Local.json"
if (Test-Path $localConfigPath) {
    $config = Get-Content $localConfigPath | ConvertFrom-Json
    if ($config.OpenAI.ApiKey -and $config.OpenAI.ApiKey -ne "YOUR_AZURE_OPENAI_API_KEY_HERE") {
        Write-Host "✓ Configuration file exists with API key" -ForegroundColor Green
    } else {
        Write-Host "✗ Configuration has placeholder API key" -ForegroundColor Red
        Write-Host "  Please update: $localConfigPath" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Configuration file missing: $localConfigPath" -ForegroundColor Red
    Write-Host "  Please create it based on CONFIGURATION.md" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Restore NuGet Packages
Write-Host "Step 4: Restoring NuGet packages..." -ForegroundColor Yellow
Set-Location src\AIQueryPlatform.Api
try {
    dotnet restore --force 2>&1 | Out-Null
    Write-Host "✓ NuGet packages restored" -ForegroundColor Green
} catch {
    Write-Host "✗ Package restore failed: $_" -ForegroundColor Red
}

Write-Host ""

# Step 5: Build Application
Write-Host "Step 5: Building application..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Application built successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Build failed" -ForegroundColor Red
        Write-Host $buildOutput
    }
} catch {
    Write-Host "✗ Build error: $_" -ForegroundColor Red
}

Write-Host ""
Set-Location $projectRoot

# Summary
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Verify your API key in: src\AIQueryPlatform.Api\appsettings.Local.json"
Write-Host "2. Run the application: cd src\AIQueryPlatform.Api; dotnet run"
Write-Host "3. Test with: Invoke-RestMethod -Uri http://localhost:7000/api/tenant/info -Headers @{'x-api-key'='demo_api_key_12345'}"
Write-Host ""
Write-Host "For troubleshooting, see: TROUBLESHOOTING.md" -ForegroundColor Cyan
Write-Host "For multi-database docs, see: MULTI_DATABASE_SUPPORT.md" -ForegroundColor Cyan
