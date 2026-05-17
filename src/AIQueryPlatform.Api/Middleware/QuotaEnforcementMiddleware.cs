using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services.Interfaces;
using System.Text.Json;

namespace AIQueryPlatform.Api.Middleware;

/// <summary>
/// Middleware to enforce token quota limits before AI requests
/// </summary>
public class QuotaEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QuotaEnforcementMiddleware> _logger;

    // Endpoints that require quota validation
    private readonly HashSet<string> _aiEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/query/execute",
        "/api/query/stream",
        "/api/conversations/",  // Conversations with /query suffix
    };

    public QuotaEnforcementMiddleware(
        RequestDelegate next,
        ILogger<QuotaEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IQuotaValidationService quotaValidationService,
        TenantContext tenantContext)
    {
        // Only validate for AI-related endpoints
        if (!ShouldValidateQuota(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check if tenant context is available
        if (!tenantContext.HasTenant)
        {
            _logger.LogWarning("No tenant context available for quota validation");
            await _next(context);
            return;
        }

        var tenantId = tenantContext.CurrentTenant!.TenantId;

        try
        {
            // Validate quota
            var validation = await quotaValidationService.ValidateQuotaAsync(tenantId);

            if (!validation.HasQuota)
            {
                // Quota exceeded - return 429 Too Many Requests
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    success = false,
                    errorCode = "TOKEN_LIMIT_EXCEEDED",
                    message = validation.Message ?? "Monthly AI token quota exceeded. Please upgrade your plan to continue.",
                    usagePercentage = validation.UsagePercentage,
                    status = validation.Status,
                    upgradeUrl = "/pricing" // TODO: Configure upgrade URL
                };

                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogWarning(
                    "Quota exceeded for tenant {TenantId}. Request blocked. Path: {Path}",
                    tenantId, context.Request.Path
                );

                await context.Response.WriteAsync(json);
                return;
            }

            // Add quota warning headers if usage is high
            if (validation.UsagePercentage >= 80)
            {
                context.Response.Headers.Add("X-Quota-Warning", validation.Message ?? "High usage");
                context.Response.Headers.Add("X-Quota-Status", validation.Status);
                context.Response.Headers.Add("X-Quota-Usage-Percentage", validation.UsagePercentage.ToString("F2"));
                context.Response.Headers.Add("X-Quota-Remaining-Tokens", validation.RemainingTokens.ToString());
            }

            // Quota available - proceed with request
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in quota enforcement middleware for tenant {TenantId}", tenantId);
            // On error, allow the request to proceed (fail open)
            await _next(context);
        }
    }

    private bool ShouldValidateQuota(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;

        // Check exact matches
        if (_aiEndpoints.Any(endpoint => pathValue.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase)))
        {
            // Special case: conversations with /query endpoint
            if (pathValue.Contains("/conversations/", StringComparison.OrdinalIgnoreCase))
            {
                return pathValue.EndsWith("/query", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        return false;
    }
}

/// <summary>
/// Extension method to add quota enforcement middleware
/// </summary>
public static class QuotaEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseQuotaEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<QuotaEnforcementMiddleware>();
    }
}
