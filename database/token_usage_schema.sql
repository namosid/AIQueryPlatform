-- ============================================
-- AI Token Usage & Quota Management Schema
-- Multi-tenant token tracking system
-- ============================================

USE AIQueryPlatform;
GO

-- ============================================
-- 1. TENANT SUBSCRIPTION TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TenantSubscriptions')
BEGIN
    CREATE TABLE TenantSubscriptions (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        PlanName NVARCHAR(100) NOT NULL DEFAULT 'Free',
        MonthlyTokenLimit BIGINT NOT NULL DEFAULT 100000,
        IsActive BIT NOT NULL DEFAULT 1,
        BillingCycleStart DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        BillingCycleEnd DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Constraints
        CONSTRAINT FK_TenantSubscriptions_Tenants FOREIGN KEY (TenantId) 
            REFERENCES Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT CK_MonthlyTokenLimit CHECK (MonthlyTokenLimit >= 0),
        CONSTRAINT UQ_TenantSubscription UNIQUE (TenantId)
    );

    -- Indexes for performance
    CREATE NONCLUSTERED INDEX IX_TenantSubscriptions_TenantId_Active 
        ON TenantSubscriptions(TenantId, IsActive);
    
    CREATE NONCLUSTERED INDEX IX_TenantSubscriptions_BillingCycle 
        ON TenantSubscriptions(BillingCycleStart, BillingCycleEnd);

    PRINT 'TenantSubscriptions table created successfully';
END
ELSE
BEGIN
    PRINT 'TenantSubscriptions table already exists';
END
GO

-- ============================================
-- 2. TOKEN USAGE TABLE (Detailed logging)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TokenUsage')
BEGIN
    CREATE TABLE TokenUsage (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ConversationId NVARCHAR(100) NULL,
        
        -- Token counts
        RequestTokens INT NOT NULL DEFAULT 0,
        ResponseTokens INT NOT NULL DEFAULT 0,
        TotalTokens INT NOT NULL DEFAULT 0,
        
        -- AI Model info
        ModelName NVARCHAR(100) NOT NULL,
        Endpoint NVARCHAR(500) NULL,
        
        -- Request metadata
        Query NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Success', -- Success, Failed, RateLimited
        ErrorMessage NVARCHAR(MAX) NULL,
        
        -- Timestamps
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Performance metrics
        ExecutionTimeMs INT NULL,
        
        -- Constraints
        CONSTRAINT FK_TokenUsage_Tenants FOREIGN KEY (TenantId) 
            REFERENCES Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT CK_TokenUsage_Tokens CHECK (
            RequestTokens >= 0 AND 
            ResponseTokens >= 0 AND 
            TotalTokens >= 0
        )
    );

    -- Indexes for high-performance queries
    CREATE NONCLUSTERED INDEX IX_TokenUsage_TenantId_CreatedDate 
        ON TokenUsage(TenantId, CreatedDate DESC) 
        INCLUDE (TotalTokens);
    
    CREATE NONCLUSTERED INDEX IX_TokenUsage_ConversationId 
        ON TokenUsage(ConversationId);
    
    CREATE NONCLUSTERED INDEX IX_TokenUsage_ModelName 
        ON TokenUsage(ModelName, CreatedDate);
    
    -- Covering index for monthly aggregations
    CREATE NONCLUSTERED INDEX IX_TokenUsage_Monthly_Aggregation 
        ON TokenUsage(TenantId, CreatedDate) 
        INCLUDE (TotalTokens, RequestTokens, ResponseTokens, ModelName);

    PRINT 'TokenUsage table created successfully';
END
ELSE
BEGIN
    PRINT 'TokenUsage table already exists';
END
GO

-- ============================================
-- 3. TOKEN USAGE SUMMARY (Cached totals)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TokenUsageSummary')
BEGIN
    CREATE TABLE TokenUsageSummary (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        
        -- Current billing cycle
        BillingCycleStart DATETIME2 NOT NULL,
        BillingCycleEnd DATETIME2 NOT NULL,
        
        -- Token counts
        CurrentMonthUsedTokens BIGINT NOT NULL DEFAULT 0,
        MonthlyTokenLimit BIGINT NOT NULL DEFAULT 100000,
        RemainingTokens BIGINT NOT NULL DEFAULT 100000,
        
        -- Usage statistics
        TotalRequests INT NOT NULL DEFAULT 0,
        SuccessfulRequests INT NOT NULL DEFAULT 0,
        FailedRequests INT NOT NULL DEFAULT 0,
        
        -- Metadata
        LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastRequestDate DATETIME2 NULL,
        
        -- Constraints
        CONSTRAINT FK_TokenUsageSummary_Tenants FOREIGN KEY (TenantId) 
            REFERENCES Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT UQ_TokenUsageSummary_Tenant UNIQUE (TenantId),
        CONSTRAINT CK_TokenUsageSummary_Tokens CHECK (
            CurrentMonthUsedTokens >= 0 AND 
            MonthlyTokenLimit >= 0 AND 
            RemainingTokens >= 0
        )
    );

    -- Indexes
    CREATE NONCLUSTERED INDEX IX_TokenUsageSummary_TenantId 
        ON TokenUsageSummary(TenantId) 
        INCLUDE (CurrentMonthUsedTokens, RemainingTokens, MonthlyTokenLimit);
    
    CREATE NONCLUSTERED INDEX IX_TokenUsageSummary_BillingCycle 
        ON TokenUsageSummary(BillingCycleStart, BillingCycleEnd);

    PRINT 'TokenUsageSummary table created successfully';
END
ELSE
BEGIN
    PRINT 'TokenUsageSummary table already exists';
END
GO

-- ============================================
-- 4. STORED PROCEDURES
-- ============================================

-- Procedure: Get current usage for tenant
IF OBJECT_ID('sp_GetTenantTokenUsage', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetTenantTokenUsage;
GO

CREATE PROCEDURE sp_GetTenantTokenUsage
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        s.CurrentMonthUsedTokens,
        s.MonthlyTokenLimit,
        s.RemainingTokens,
        CAST(ROUND((CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0)) * 100, 2) AS DECIMAL(5,2)) AS UsagePercentage,
        s.TotalRequests,
        s.SuccessfulRequests,
        s.FailedRequests,
        s.BillingCycleStart,
        s.BillingCycleEnd,
        s.LastUpdated,
        sub.PlanName,
        sub.IsActive,
        CASE
            WHEN s.CurrentMonthUsedTokens >= s.MonthlyTokenLimit THEN 'exceeded'
            WHEN CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0) >= 0.95 THEN 'critical'
            WHEN CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0) >= 0.80 THEN 'warning'
            ELSE 'normal'
        END AS Status
    FROM 
        TokenUsageSummary s
        INNER JOIN TenantSubscriptions sub ON s.TenantId = sub.TenantId
    WHERE 
        s.TenantId = @TenantId
        AND sub.IsActive = 1;
END
GO

-- Procedure: Record token usage
IF OBJECT_ID('sp_RecordTokenUsage', 'P') IS NOT NULL
    DROP PROCEDURE sp_RecordTokenUsage;
GO

CREATE PROCEDURE sp_RecordTokenUsage
    @TenantId UNIQUEIDENTIFIER,
    @ConversationId NVARCHAR(100) = NULL,
    @RequestTokens INT,
    @ResponseTokens INT,
    @TotalTokens INT,
    @ModelName NVARCHAR(100),
    @Endpoint NVARCHAR(500) = NULL,
    @Query NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(50) = 'Success',
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @ExecutionTimeMs INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Insert detailed usage record
        INSERT INTO TokenUsage (
            TenantId, ConversationId, RequestTokens, ResponseTokens, TotalTokens,
            ModelName, Endpoint, Query, Status, ErrorMessage, ExecutionTimeMs, CreatedDate
        )
        VALUES (
            @TenantId, @ConversationId, @RequestTokens, @ResponseTokens, @TotalTokens,
            @ModelName, @Endpoint, @Query, @Status, @ErrorMessage, @ExecutionTimeMs, GETUTCDATE()
        );
        
        -- Update summary (UPSERT)
        MERGE TokenUsageSummary AS target
        USING (
            SELECT 
                @TenantId AS TenantId,
                sub.BillingCycleStart,
                sub.BillingCycleEnd,
                sub.MonthlyTokenLimit
            FROM TenantSubscriptions sub
            WHERE sub.TenantId = @TenantId AND sub.IsActive = 1
        ) AS source
        ON target.TenantId = source.TenantId
        WHEN MATCHED THEN
            UPDATE SET
                CurrentMonthUsedTokens = target.CurrentMonthUsedTokens + @TotalTokens,
                RemainingTokens = source.MonthlyTokenLimit - (target.CurrentMonthUsedTokens + @TotalTokens),
                TotalRequests = target.TotalRequests + 1,
                SuccessfulRequests = target.SuccessfulRequests + CASE WHEN @Status = 'Success' THEN 1 ELSE 0 END,
                FailedRequests = target.FailedRequests + CASE WHEN @Status != 'Success' THEN 1 ELSE 0 END,
                LastUpdated = GETUTCDATE(),
                LastRequestDate = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (
                TenantId, BillingCycleStart, BillingCycleEnd, CurrentMonthUsedTokens,
                MonthlyTokenLimit, RemainingTokens, TotalRequests, SuccessfulRequests,
                FailedRequests, LastUpdated, LastRequestDate
            )
            VALUES (
                source.TenantId, source.BillingCycleStart, source.BillingCycleEnd, @TotalTokens,
                source.MonthlyTokenLimit, source.MonthlyTokenLimit - @TotalTokens, 1,
                CASE WHEN @Status = 'Success' THEN 1 ELSE 0 END,
                CASE WHEN @Status != 'Success' THEN 1 ELSE 0 END,
                GETUTCDATE(), GETUTCDATE()
            );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Procedure: Check if tenant has quota
IF OBJECT_ID('sp_CheckTenantQuota', 'P') IS NOT NULL
    DROP PROCEDURE sp_CheckTenantQuota;
GO

CREATE PROCEDURE sp_CheckTenantQuota
    @TenantId UNIQUEIDENTIFIER,
    @HasQuota BIT OUTPUT,
    @RemainingTokens BIGINT OUTPUT,
    @UsagePercentage DECIMAL(5,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        @HasQuota = CASE WHEN s.RemainingTokens > 0 THEN 1 ELSE 0 END,
        @RemainingTokens = s.RemainingTokens,
        @UsagePercentage = CAST(ROUND((CAST(s.CurrentMonthUsedTokens AS FLOAT) / NULLIF(s.MonthlyTokenLimit, 0)) * 100, 2) AS DECIMAL(5,2))
    FROM 
        TokenUsageSummary s
        INNER JOIN TenantSubscriptions sub ON s.TenantId = sub.TenantId
    WHERE 
        s.TenantId = @TenantId
        AND sub.IsActive = 1;
    
    -- If no record exists, assume quota available
    IF @HasQuota IS NULL
    BEGIN
        SET @HasQuota = 1;
        SET @RemainingTokens = 100000; -- Default limit
        SET @UsagePercentage = 0;
    END
END
GO

-- ============================================
-- 5. INITIALIZE DEFAULT SUBSCRIPTIONS
-- ============================================

-- Add subscriptions for existing tenants
INSERT INTO TenantSubscriptions (TenantId, PlanName, MonthlyTokenLimit, BillingCycleStart, BillingCycleEnd)
SELECT 
    t.TenantId,
    'Free' AS PlanName,
    100000 AS MonthlyTokenLimit,
    DATEADD(DAY, -DAY(GETUTCDATE())+1, CAST(GETUTCDATE() AS DATE)) AS BillingCycleStart,
    DATEADD(DAY, -1, DATEADD(MONTH, 1, DATEADD(DAY, -DAY(GETUTCDATE())+1, CAST(GETUTCDATE() AS DATE)))) AS BillingCycleEnd
FROM 
    Tenants t
WHERE 
    NOT EXISTS (SELECT 1 FROM TenantSubscriptions WHERE TenantId = t.TenantId);

PRINT 'Default subscriptions initialized for existing tenants';
GO

-- ============================================
-- 6. VIEWS FOR ANALYTICS
-- ============================================

-- Daily usage trend
IF OBJECT_ID('vw_DailyTokenUsage', 'V') IS NOT NULL
    DROP VIEW vw_DailyTokenUsage;
GO

CREATE VIEW vw_DailyTokenUsage AS
SELECT 
    TenantId,
    CAST(CreatedDate AS DATE) AS UsageDate,
    COUNT(*) AS RequestCount,
    SUM(TotalTokens) AS TotalTokens,
    SUM(RequestTokens) AS TotalRequestTokens,
    SUM(ResponseTokens) AS TotalResponseTokens,
    AVG(ExecutionTimeMs) AS AvgExecutionTimeMs,
    COUNT(DISTINCT ConversationId) AS UniqueConversations
FROM 
    TokenUsage
WHERE
    Status = 'Success'
GROUP BY 
    TenantId,
    CAST(CreatedDate AS DATE);
GO

-- Model usage breakdown
IF OBJECT_ID('vw_ModelUsageBreakdown', 'V') IS NOT NULL
    DROP VIEW vw_ModelUsageBreakdown;
GO

CREATE VIEW vw_ModelUsageBreakdown AS
SELECT 
    TenantId,
    ModelName,
    COUNT(*) AS RequestCount,
    SUM(TotalTokens) AS TotalTokens,
    AVG(TotalTokens) AS AvgTokensPerRequest,
    MAX(CreatedDate) AS LastUsedDate
FROM 
    TokenUsage
WHERE
    Status = 'Success'
GROUP BY 
    TenantId,
    ModelName;
GO

PRINT '============================================';
PRINT 'Token Usage & Quota Management Schema Created Successfully';
PRINT '============================================';
PRINT '';
PRINT 'Tables Created:';
PRINT '  - TenantSubscriptions';
PRINT '  - TokenUsage';
PRINT '  - TokenUsageSummary';
PRINT '';
PRINT 'Stored Procedures Created:';
PRINT '  - sp_GetTenantTokenUsage';
PRINT '  - sp_RecordTokenUsage';
PRINT '  - sp_CheckTenantQuota';
PRINT '';
PRINT 'Views Created:';
PRINT '  - vw_DailyTokenUsage';
PRINT '  - vw_ModelUsageBreakdown';
PRINT '============================================';
