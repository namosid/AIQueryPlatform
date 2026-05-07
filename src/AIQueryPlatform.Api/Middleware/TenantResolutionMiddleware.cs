using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services.Interfaces;

namespace AIQueryPlatform.Api.Middleware;

/// <summary>
/// Middleware to resolve tenant from request headers (x-api-key or x-tenant-id)
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantService tenantService,
        TenantContext tenantContext)
    {
        try
        {
            // Try to get API key from header
            if (context.Request.Headers.TryGetValue("x-api-key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
            {
                var tenant = await tenantService.GetTenantByApiKeyAsync(apiKey!);
                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant);
                    _logger.LogInformation("Tenant resolved: {TenantId} - {TenantName}", tenant.TenantId, tenant.Name);
                }
                else
                {
                    _logger.LogWarning("Invalid or inactive API key provided");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive API key" });
                    return;
                }
            }
            // Try to get tenant ID from header
            else if (context.Request.Headers.TryGetValue("x-tenant-id", out var tenantId) && !string.IsNullOrEmpty(tenantId))
            {
                if (Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    var tenant = await tenantService.GetTenantByIdAsync(parsedTenantId);
                    if (tenant != null && tenant.IsActive)
                    {
                        tenantContext.SetTenant(tenant);
                        _logger.LogInformation("Tenant resolved: {TenantId} - {TenantName}", tenant.TenantId, tenant.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid or inactive tenant ID: {TenantId}", parsedTenantId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive tenant" });
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid tenant ID format: {TenantId}", tenantId);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant ID format" });
                    return;
                }
            }
            // Skip tenant resolution for health check and swagger endpoints
            else if (context.Request.Path.StartsWithSegments("/health") ||
                     context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }
            else
            {
                _logger.LogWarning("No tenant authentication provided");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Missing tenant authentication (x-api-key or x-tenant-id header required)" });
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant resolution");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}
