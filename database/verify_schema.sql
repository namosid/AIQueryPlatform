-- Verify Tenant Table Schema
-- Run this to check if the database has been updated with multi-database support columns

USE AIQueryPlatform;
GO

-- Check if DatabaseType and DatabaseSettings columns exist
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Tenants'
ORDER BY ORDINAL_POSITION;
GO

-- Check current tenant configuration
SELECT 
    TenantId,
    Name,
    ApiKey,
    DatabaseType,
    CASE DatabaseType
        WHEN 0 THEN 'SQL Server'
        WHEN 1 THEN 'MySQL'
        WHEN 2 THEN 'PostgreSQL'
        WHEN 3 THEN 'Excel'
        WHEN 4 THEN 'SQLite'
        ELSE 'Unknown'
    END AS DatabaseTypeName,
    DatabaseSettings,
    IsActive
FROM Tenants;
GO
