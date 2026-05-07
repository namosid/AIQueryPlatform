namespace AIQueryPlatform.Api.Models;

/// <summary>
/// Supported database types for tenants
/// </summary>
public enum DatabaseType
{
    SqlServer = 0,
    MySql = 1,
    PostgreSql = 2,
    Excel = 3,
    Sqlite = 4
}

/// <summary>
/// Database configuration settings for a tenant
/// </summary>
public class DatabaseSettings
{
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    public string ConnectionString { get; set; } = string.Empty;
    
    // Excel-specific settings
    public string? ExcelFilePath { get; set; }
    public string? ExcelSheetName { get; set; }
    
    // Additional settings
    public int CommandTimeout { get; set; } = 30;
    public bool UseConnectionPooling { get; set; } = true;
}
