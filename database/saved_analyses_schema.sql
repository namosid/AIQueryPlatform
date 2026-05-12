-- Saved Analyses Schema
-- Run this script to create the SavedAnalyses table

USE AIQueryPlatform;
GO

-- Create SavedAnalyses table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedAnalyses')
BEGIN
    CREATE TABLE SavedAnalyses (
        Id NVARCHAR(100) PRIMARY KEY,
        Title NVARCHAR(500) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        ConversationId NVARCHAR(100) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        SavedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SavedAnalyses_Conversations FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SavedAnalyses_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE NO ACTION
    );

    CREATE INDEX IX_SavedAnalyses_TenantId ON SavedAnalyses(TenantId);
    CREATE INDEX IX_SavedAnalyses_SavedAt ON SavedAnalyses(SavedAt DESC);
    CREATE INDEX IX_SavedAnalyses_ConversationId ON SavedAnalyses(ConversationId);
    
    PRINT 'SavedAnalyses table created successfully';
END
ELSE
BEGIN
    PRINT 'SavedAnalyses table already exists';
END
GO

PRINT 'Saved Analyses schema setup complete';
