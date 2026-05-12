using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIQueryPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysesController : ControllerBase
{
    private readonly ISavedAnalysisService _savedAnalysisService;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<AnalysesController> _logger;

    public AnalysesController(
        ISavedAnalysisService savedAnalysisService,
        TenantContext tenantContext,
        ILogger<AnalysesController> logger)
    {
        _savedAnalysisService = savedAnalysisService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// List all saved analyses for the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SavedAnalysisListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SavedAnalysisListResponse>> ListSavedAnalysesAsync()
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var response = await _savedAnalysisService.ListSavedAnalysesAsync(tenantId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing saved analyses for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to list saved analyses" });
        }
    }

    /// <summary>
    /// Get a specific saved analysis by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SavedAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SavedAnalysisDto>> GetSavedAnalysisAsync(string id)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var analysis = await _savedAnalysisService.GetSavedAnalysisAsync(id, tenantId);

            if (analysis == null)
            {
                return NotFound(new { error = "Saved analysis not found" });
            }

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved analysis {AnalysisId}", id);
            return StatusCode(500, new { error = "Failed to get saved analysis" });
        }
    }

    /// <summary>
    /// Save an analysis from a conversation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SavedAnalysisDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SavedAnalysisDto>> SaveAnalysisAsync(
        [FromBody] SaveAnalysisRequest request)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return BadRequest(new { error = "ConversationId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title is required" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var analysis = await _savedAnalysisService.SaveAnalysisAsync(
                request.ConversationId,
                tenantId,
                request.Title,
                request.Description
            );

            _logger.LogInformation("Analysis {AnalysisId} saved successfully for conversation {ConversationId}", 
                analysis.Id, request.ConversationId);

            // Return 201 Created with the analysis data
            // Use StatusCode instead of CreatedAtAction to avoid URL generation issues
            return StatusCode(StatusCodes.Status201Created, analysis);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving analysis for conversation {ConversationId}", request.ConversationId);
            return StatusCode(500, new { error = "Failed to save analysis" });
        }
    }

    /// <summary>
    /// Delete a saved analysis
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSavedAnalysisAsync(string id)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized(new { error = "No tenant context" });
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;

        try
        {
            var deleted = await _savedAnalysisService.DeleteSavedAnalysisAsync(id, tenantId);

            if (!deleted)
            {
                return NotFound(new { error = "Saved analysis not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved analysis {AnalysisId}", id);
            return StatusCode(500, new { error = "Failed to delete saved analysis" });
        }
    }
}
