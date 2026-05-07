using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for retrieving and caching database schemas
/// </summary>
public class SchemaService : ISchemaService
{
    private readonly ILogger<SchemaService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    public SchemaService(
        ILogger<SchemaService> logger,
        IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<DatabaseSchema> GetDatabaseSchemaAsync(string connectionString, Guid tenantId)
    {
        var cacheKey = $"schema_{tenantId}";

        if (_cache.TryGetValue<DatabaseSchema>(cacheKey, out var cachedSchema) && cachedSchema != null)
        {
            _logger.LogInformation("Returning cached schema for tenant {TenantId}", tenantId);
            return cachedSchema;
        }

        _logger.LogInformation("Loading database schema for tenant {TenantId}", tenantId);

        var schema = new DatabaseSchema
        {
            TenantId = tenantId.ToString(),
            Tables = new List<TableSchema>()
        };

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get all user tables
            var tablesQuery = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                AND TABLE_SCHEMA != 'sys'
                ORDER BY TABLE_NAME";

            using var tablesCommand = new SqlCommand(tablesQuery, connection);
            using var tablesReader = await tablesCommand.ExecuteReaderAsync();

            var tables = new List<(string Schema, string Name)>();
            while (await tablesReader.ReadAsync())
            {
                tables.Add((
                    tablesReader.GetString(0),
                    tablesReader.GetString(1)
                ));
            }

            await tablesReader.CloseAsync();

            // Get columns for each table
            foreach (var (tableSchema, tableName) in tables)
            {
                var table = new TableSchema
                {
                    TableName = $"{tableSchema}.{tableName}",
                    Columns = new List<ColumnSchema>()
                };

                var columnsQuery = @"
                    SELECT 
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.IS_NULLABLE,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN (
                        SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                            ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                        AND c.TABLE_NAME = pk.TABLE_NAME 
                        AND c.COLUMN_NAME = pk.COLUMN_NAME
                    WHERE c.TABLE_SCHEMA = @TableSchema AND c.TABLE_NAME = @TableName
                    ORDER BY c.ORDINAL_POSITION";

                using var columnsCommand = new SqlCommand(columnsQuery, connection);
                columnsCommand.Parameters.AddWithValue("@TableSchema", tableSchema);
                columnsCommand.Parameters.AddWithValue("@TableName", tableName);

                using var columnsReader = await columnsCommand.ExecuteReaderAsync();
                
                while (await columnsReader.ReadAsync())
                {
                    var column = new ColumnSchema
                    {
                        ColumnName = columnsReader.GetString(0),
                        DataType = columnsReader.GetString(1),
                        IsNullable = columnsReader.GetString(2) == "YES",
                        IsPrimaryKey = columnsReader.GetInt32(3) == 1
                    };

                    table.Columns.Add(column);
                }

                await columnsReader.CloseAsync();

                if (table.Columns.Any())
                {
                    schema.Tables.Add(table);
                }
            }

            _logger.LogInformation("Loaded schema with {TableCount} tables for tenant {TenantId}", 
                schema.Tables.Count, tenantId);

            // Cache the schema
            _cache.Set(cacheKey, schema, CacheDuration);

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading database schema for tenant {TenantId}", tenantId);
            throw new InvalidOperationException("Failed to load database schema", ex);
        }
    }

    public Task InvalidateCacheAsync(Guid tenantId)
    {
        var cacheKey = $"schema_{tenantId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Invalidated schema cache for tenant {TenantId}", tenantId);
        return Task.CompletedTask;
    }
}
