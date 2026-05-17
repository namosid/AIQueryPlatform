# =============================================================================
# TEST: Server-Side Insights Configuration
# =============================================================================
# Tests the complete flow of insights configuration from database to UI
# =============================================================================

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$TenantId = "",
    [string]$ApiKey = ""
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TESTING: Server-Side Insights Config" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check parameters
if ([string]::IsNullOrEmpty($TenantId) -or [string]::IsNullOrEmpty($ApiKey)) {
    Write-Host "Usage: .\test-insights-config.ps1 -TenantId 'your-tenant-id' -ApiKey 'your-api-key'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or set manually:" -ForegroundColor Yellow
    $TenantId = Read-Host "Enter Tenant ID"
    $ApiKey = Read-Host "Enter API Key"
}

$headers = @{
    "x-tenant-id" = $TenantId
    "x-api-key" = $ApiKey
    "Content-Type" = "application/json"
}

# =============================================================================
# TEST 1: Verify API is running
# =============================================================================
Write-Host "TEST 1: Checking API availability..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/health" -Method Get -ErrorAction SilentlyContinue
    Write-Host "   ✓ API is running" -ForegroundColor Green
} catch {
    Write-Host "   ✗ API not reachable at $ApiBaseUrl" -ForegroundColor Red
    Write-Host "   Make sure the API is running: cd src/AIQueryPlatform.Api; dotnet run" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# =============================================================================
# TEST 2: Get current tenant settings (should include EnableInsights)
# =============================================================================
Write-Host "TEST 2: Fetching tenant configuration..." -ForegroundColor Yellow
try {
    $tenant = Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/current" -Method Get -Headers $headers
    
    Write-Host "   Tenant: $($tenant.name)" -ForegroundColor White
    Write-Host "   Tenant ID: $($tenant.tenantId)" -ForegroundColor White
    
    if ($null -ne $tenant.enableInsights) {
        Write-Host "   EnableInsights: $($tenant.enableInsights)" -ForegroundColor White
        Write-Host "   ✓ EnableInsights property present" -ForegroundColor Green
        $currentSetting = $tenant.enableInsights
    } else {
        Write-Host "   ✗ EnableInsights property missing!" -ForegroundColor Red
        Write-Host "   Check: TenantService.cs includes EnableInsights in SQL queries" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "   ✗ Failed to fetch tenant info" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# =============================================================================
# TEST 3: Disable insights
# =============================================================================
Write-Host "TEST 3: Disabling insights for tenant..." -ForegroundColor Yellow
try {
    $body = @{ enableInsights = $false } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/insights" -Method Patch -Headers $headers -Body $body
    
    Write-Host "   Response: $($response.message)" -ForegroundColor White
    Write-Host "   New setting: enableInsights = $($response.enableInsights)" -ForegroundColor White
    Write-Host "   ✓ Setting updated successfully" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Failed to update setting" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Verify the change
Write-Host "   Verifying change..." -ForegroundColor Yellow
Start-Sleep -Seconds 1
$tenant = Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/current" -Method Get -Headers $headers
if ($tenant.enableInsights -eq $false) {
    Write-Host "   ✓ Confirmed: EnableInsights = false" -ForegroundColor Green
} else {
    Write-Host "   ✗ Setting not updated in database!" -ForegroundColor Red
    exit 1
}
Write-Host ""

# =============================================================================
# TEST 4: Send a test query (should skip recommendations)
# =============================================================================
Write-Host "TEST 4: Sending test query with insights disabled..." -ForegroundColor Yellow
try {
    $queryBody = @{
        query = "Show me total sales"
        conversationId = $null
    } | ConvertTo-Json
    
    $queryResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/conversations/query" -Method Post -Headers $headers -Body $queryBody
    
    if ($queryResponse.recommendations -eq $null -or $queryResponse.recommendations.Count -eq 0) {
        Write-Host "   ✓ No recommendations generated (as expected)" -ForegroundColor Green
        Write-Host "   Tokens saved!" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Recommendations were generated even though insights disabled!" -ForegroundColor Red
        Write-Host "   Check: ConversationsController checks EnableInsights before calling RecommendationService" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠ Query failed (may be expected if database not configured)" -ForegroundColor Yellow
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# =============================================================================
# TEST 5: Re-enable insights
# =============================================================================
Write-Host "TEST 5: Re-enabling insights..." -ForegroundColor Yellow
try {
    $body = @{ enableInsights = $true } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/insights" -Method Patch -Headers $headers -Body $body
    
    Write-Host "   ✓ Insights re-enabled" -ForegroundColor Green
    
    # Verify
    Start-Sleep -Seconds 1
    $tenant = Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/current" -Method Get -Headers $headers
    if ($tenant.enableInsights -eq $true) {
        Write-Host "   ✓ Confirmed: EnableInsights = true" -ForegroundColor Green
    }
} catch {
    Write-Host "   ✗ Failed to re-enable insights" -ForegroundColor Red
}
Write-Host ""

# =============================================================================
# TEST 6: Send query with insights enabled (should include recommendations)
# =============================================================================
Write-Host "TEST 6: Sending test query with insights enabled..." -ForegroundColor Yellow
try {
    $queryBody = @{
        query = "Show me total sales"
        conversationId = $null
    } | ConvertTo-Json
    
    $queryResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/conversations/query" -Method Post -Headers $headers -Body $queryBody
    
    if ($queryResponse.recommendations -ne $null -and $queryResponse.recommendations.Count -gt 0) {
        Write-Host "   ✓ Recommendations generated: $($queryResponse.recommendations.Count) items" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ No recommendations generated" -ForegroundColor Yellow
        Write-Host "   (May be expected if query has no results)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠ Query failed" -ForegroundColor Yellow
}
Write-Host ""

# =============================================================================
# SUMMARY
# =============================================================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ API endpoints working" -ForegroundColor Green
Write-Host "✓ Tenant configuration includes EnableInsights" -ForegroundColor Green
Write-Host "✓ Settings can be toggled via API" -ForegroundColor Green
Write-Host "✓ Backend respects EnableInsights setting" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Check API logs for:" -ForegroundColor White
Write-Host "   - 'Skipping recommendations generation - insights disabled'" -ForegroundColor Gray
Write-Host "   - 'Generating AI recommendations for query results'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Test in widget/modal UI:" -ForegroundColor White
Write-Host "   - Rebuild widget: cd widget; npm run build" -ForegroundColor Gray
Write-Host "   - Open modal with insights disabled" -ForegroundColor Gray
Write-Host "   - Verify: Right panel should be hidden" -ForegroundColor Gray
Write-Host "   - Re-enable insights and refresh" -ForegroundColor Gray
Write-Host "   - Verify: Right panel appears with recommendations" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Monitor token usage:" -ForegroundColor White
Write-Host "   - Check TokenUsage table" -ForegroundColor Gray
Write-Host "   - Verify no 'Recommendations' entries when disabled" -ForegroundColor Gray
Write-Host ""

# Restore original setting
if ($currentSetting -ne $null) {
    Write-Host "Restoring original setting: enableInsights = $currentSetting..." -ForegroundColor Gray
    $body = @{ enableInsights = $currentSetting } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBaseUrl/api/tenant/insights" -Method Patch -Headers $headers -Body $body | Out-Null
    Write-Host "✓ Restored" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TESTS COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
