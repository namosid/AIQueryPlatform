namespace AIQueryPlatform.Api.Models;

/// <summary>
/// Represents a tenant in the multi-tenant system
/// </summary>
public class Tenant
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    
    // Database configuration
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    public string? DatabaseSettings { get; set; } // JSON string for additional settings
    
    // UI customization
    public string? LogoUrl { get; set; }
    public string? ThemeColor { get; set; }
    
    // Feature flags
    public bool EnableInsights { get; set; } = true; // Control AI recommendations/insights generation
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? SchemaFile { get; set; }
}
