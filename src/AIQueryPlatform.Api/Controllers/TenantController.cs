using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIQueryPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly TenantContext _tenantContext;
    private readonly ISchemaService _schemaService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ITenantService tenantService,
        TenantContext tenantContext,
        ISchemaService schemaService,
        ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _schemaService = schemaService;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant information
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> GetCurrent()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenant = _tenantContext.CurrentTenant!;

        // Return sanitized tenant info (without sensitive data)
        return Ok(new
        {
            tenant.TenantId,
            tenant.Name,
            tenant.LogoUrl,
            tenant.ThemeColor,
            tenant.IsActive
        });
    }

    /// <summary>
    /// Invalidate schema cache for current tenant
    /// </summary>
    [HttpPost("invalidate-cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InvalidateCache()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        await _schemaService.InvalidateCacheAsync(_tenantContext.TenantId);

        return Ok(new { message = "Schema cache invalidated successfully" });
    }

    /// <summary>
    /// Health check for current tenant
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            status = "healthy",
            hasTenant = _tenantContext.HasTenant,
            tenantId = _tenantContext.HasTenant ? (Guid?)_tenantContext.TenantId : null,
            timestamp = DateTime.UtcNow
        });
    }
}
