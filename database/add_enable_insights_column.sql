-- ============================================
-- Add EnableInsights Column to Tenants Table
-- Controls whether AI insights/recommendations are generated for tenant
-- ============================================

USE AIQueryPlatform;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Tenants') 
    AND name = 'EnableInsights'
)
BEGIN
    PRINT 'Adding EnableInsights column to Tenants table...';
    
    -- Add the column with default value of 1 (enabled)
    ALTER TABLE Tenants
    ADD EnableInsights BIT NOT NULL DEFAULT 1;
    
    PRINT 'EnableInsights column added successfully';
    PRINT 'Default: Insights ENABLED for all existing tenants';
END
ELSE
BEGIN
    PRINT 'EnableInsights column already exists';
END
GO

-- Display current tenant insights settings
SELECT 
    TenantId,
    Name,
    EnableInsights,
    CASE 
        WHEN EnableInsights = 1 THEN 'Recommendations will be generated (tokens consumed)'
        ELSE 'Recommendations disabled (tokens saved)'
    END AS Status
FROM Tenants;
GO

-- Example: Disable insights for a specific tenant
-- UNCOMMENT and replace with actual TenantId to disable
/*
UPDATE Tenants
SET EnableInsights = 0
WHERE TenantId = 'YOUR-TENANT-ID-HERE';
GO

PRINT 'Insights disabled for tenant';
*/

PRINT 'Migration completed successfully';
GO
