namespace AIQueryPlatform.Api.Models;

/// <summary>
/// Scoped service that holds the current tenant context for the request
/// </summary>
public class TenantContext
{
    public Tenant? CurrentTenant { get; set; }

    public bool HasTenant => CurrentTenant != null;

    public Guid TenantId => CurrentTenant?.TenantId ?? Guid.Empty;

    public string TenantName => CurrentTenant?.Name ?? "Unknown";

    public void SetTenant(Tenant tenant)
    {
        CurrentTenant = tenant;
    }

    public void Clear()
    {
        CurrentTenant = null;
    }
}
