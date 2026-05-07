using AIQueryPlatform.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AIQueryPlatform.Api.Middleware;

/// <summary>
/// Middleware to enforce rate limiting per tenant
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly int _requestsPerMinute;
    private readonly int _requestsPerHour;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _requestsPerMinute = configuration.GetValue<int>("RateLimiting:RequestsPerMinute", 60);
        _requestsPerHour = configuration.GetValue<int>("RateLimiting:RequestsPerHour", 1000);
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Skip rate limiting for health check and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!tenantContext.HasTenant)
        {
            await _next(context);
            return;
        }

        var tenantId = tenantContext.TenantId;
        var cacheKey = $"ratelimit_{tenantId}";
        
        var rateLimitInfo = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return new RateLimitInfo
            {
                TenantId = tenantId,
                MinuteWindow = new Queue<DateTime>(),
                HourWindow = new Queue<DateTime>()
            };
        });

        if (rateLimitInfo == null)
        {
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;

        // Clean up old entries
        while (rateLimitInfo.MinuteWindow.Count > 0 && 
               rateLimitInfo.MinuteWindow.Peek() < now.AddMinutes(-1))
        {
            rateLimitInfo.MinuteWindow.Dequeue();
        }

        while (rateLimitInfo.HourWindow.Count > 0 && 
               rateLimitInfo.HourWindow.Peek() < now.AddHours(-1))
        {
            rateLimitInfo.HourWindow.Dequeue();
        }

        // Check rate limits
        if (rateLimitInfo.MinuteWindow.Count >= _requestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} - per minute limit", tenantId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Rate limit exceeded",
                limit = _requestsPerMinute,
                window = "1 minute",
                retryAfter = 60
            });
            return;
        }

        if (rateLimitInfo.HourWindow.Count >= _requestsPerHour)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} - per hour limit", tenantId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "3600");
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Rate limit exceeded",
                limit = _requestsPerHour,
                window = "1 hour",
                retryAfter = 3600
            });
            return;
        }

        // Record this request
        rateLimitInfo.MinuteWindow.Enqueue(now);
        rateLimitInfo.HourWindow.Enqueue(now);
        rateLimitInfo.LastRequestTime = now;

        // Update cache
        _cache.Set(cacheKey, rateLimitInfo, TimeSpan.FromHours(1));

        await _next(context);
    }
}
