using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIQueryPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenUsageController : ControllerBase
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IQuotaValidationService _quotaValidationService;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<TokenUsageController> _logger;

    public TokenUsageController(
        ITokenUsageService tokenUsageService,
        IQuotaValidationService quotaValidationService,
        TenantContext tenantContext,
        ILogger<TokenUsageController> logger)
    {
        _tokenUsageService = tokenUsageService;
        _quotaValidationService = quotaValidationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get current token usage for the tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TokenUsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenUsageResponse>> GetTokenUsageAsync()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var usage = await _tokenUsageService.GetTokenUsageAsync(tenantId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token usage for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to get token usage" });
        }
    }

    /// <summary>
    /// Get daily usage trend
    /// </summary>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(List<DailyUsageTrend>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DailyUsageTrend>>> GetDailyTrendAsync([FromQuery] int days = 30)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var trend = await _tokenUsageService.GetDailyUsageTrendAsync(tenantId, days);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage trend for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to get usage trend" });
        }
    }

    /// <summary>
    /// Get usage analytics
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(UsageAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageAnalyticsResponse>> GetAnalyticsAsync()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var analytics = await _tokenUsageService.GetUsageAnalyticsAsync(tenantId);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to get analytics" });
        }
    }

    /// <summary>
    /// Check quota status
    /// </summary>
    [HttpGet("quota/status")]
    [ProducesResponseType(typeof(QuotaValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<QuotaValidationResult>> CheckQuotaAsync()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var validation = await _quotaValidationService.ValidateQuotaAsync(tenantId);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quota for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to check quota" });
        }
    }

    /// <summary>
    /// Admin: Get all tenants usage
    /// </summary>
    [HttpGet("admin/all-tenants")]
    [ProducesResponseType(typeof(List<TenantUsageOverview>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantUsageOverview>>> GetAllTenantsUsageAsync()
    {
        // TODO: Add admin authorization check
        
        try
        {
            var overviews = await _tokenUsageService.GetAllTenantsUsageAsync();
            return Ok(overviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants usage");
            return StatusCode(500, new { error = "Failed to get tenants usage" });
        }
    }

    /// <summary>
    /// Admin: Reset monthly usage for a tenant
    /// </summary>
    [HttpPost("admin/{tenantId:guid}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetMonthlyUsageAsync(Guid tenantId)
    {
        // TODO: Add admin authorization check
        
        try
        {
            await _tokenUsageService.ResetMonthlyUsageAsync(tenantId);
            return Ok(new { message = "Monthly usage reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting usage for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to reset usage" });
        }
    }

    /// <summary>
    /// Admin: Update tenant subscription
    /// </summary>
    [HttpPut("admin/{tenantId:guid}/subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSubscriptionAsync(
        Guid tenantId,
        [FromBody] UpdateSubscriptionRequest request)
    {
        // TODO: Add admin authorization check
        
        try
        {
            await _tokenUsageService.UpdateSubscriptionAsync(tenantId, request);
            return Ok(new { message = "Subscription updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to update subscription" });
        }
    }
}
