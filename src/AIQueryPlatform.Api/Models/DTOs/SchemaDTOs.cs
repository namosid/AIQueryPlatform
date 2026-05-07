namespace AIQueryPlatform.Api.Models.DTOs;

/// <summary>
/// Database schema information
/// </summary>
public class DatabaseSchema
{
    public string TenantId { get; set; } = string.Empty;
    public List<TableSchema> Tables { get; set; } = new();
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Table schema with columns
/// </summary>
public class TableSchema
{
    public string TableName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ColumnSchema> Columns { get; set; } = new();
}

/// <summary>
/// Column schema with metadata
/// </summary>
public class ColumnSchema
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? Description { get; set; }
    public List<string> Synonyms { get; set; } = new();
}
