-- =============================================================================
-- VERIFY SERVER-SIDE INSIGHTS CONFIGURATION
-- =============================================================================
-- This script verifies the EnableInsights feature is properly configured
-- Run after applying the add_enable_insights_column.sql migration
-- =============================================================================

PRINT '========================================';
PRINT 'VERIFICATION: EnableInsights Feature';
PRINT '========================================';
PRINT '';

-- 1. Check column exists
PRINT '1. Checking if EnableInsights column exists...';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Tenants') 
    AND name = 'EnableInsights'
)
BEGIN
    PRINT '   ✓ Column exists';
    
    -- Check column properties
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.is_nullable AS IsNullable,
        dc.definition AS DefaultValue
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('Tenants')
    AND c.name = 'EnableInsights';
    
    PRINT '';
END
ELSE
BEGIN
    PRINT '   ✗ Column NOT FOUND - Run migration script first!';
    PRINT '';
    RETURN;
END

-- 2. Check current tenant settings
PRINT '2. Current tenant insights settings:';
PRINT '';

SELECT 
    TenantId,
    Name,
    EnableInsights,
    CASE 
        WHEN EnableInsights = 1 THEN '✓ ENABLED (AI recommendations will be generated)'
        ELSE '✗ DISABLED (Tokens saved, no recommendations)'
    END AS Status,
    IsActive
FROM Tenants
ORDER BY Name;

PRINT '';

-- 3. Summary statistics
PRINT '3. Summary:';
DECLARE @TotalTenants INT;
DECLARE @EnabledCount INT;
DECLARE @DisabledCount INT;

SELECT @TotalTenants = COUNT(*) FROM Tenants WHERE IsActive = 1;
SELECT @EnabledCount = COUNT(*) FROM Tenants WHERE IsActive = 1 AND EnableInsights = 1;
SELECT @DisabledCount = COUNT(*) FROM Tenants WHERE IsActive = 1 AND EnableInsights = 0;

PRINT '   Total active tenants: ' + CAST(@TotalTenants AS VARCHAR(10));
PRINT '   Insights enabled:     ' + CAST(@EnabledCount AS VARCHAR(10));
PRINT '   Insights disabled:    ' + CAST(@DisabledCount AS VARCHAR(10));

IF @DisabledCount > 0
BEGIN
    DECLARE @PotentialSavings INT = @DisabledCount * 500; -- Estimated 500 tokens saved per query
    PRINT '';
    PRINT '   Estimated token savings: ~' + CAST(@PotentialSavings AS VARCHAR(10)) + ' tokens per query round';
END

PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '========================================';
