-- Conversations and Messages Schema
-- Run this script to create tables for conversation history

USE AIQueryPlatform;
GO

-- Create Conversations table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Conversations')
BEGIN
    CREATE TABLE Conversations (
        Id NVARCHAR(100) PRIMARY KEY,
        Title NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsPinned BIT NOT NULL DEFAULT 0,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_Conversations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Conversations_TenantId ON Conversations(TenantId);
    CREATE INDEX IX_Conversations_UpdatedAt ON Conversations(UpdatedAt DESC);
    CREATE INDEX IX_Conversations_IsPinned ON Conversations(IsPinned);
    
    PRINT 'Conversations table created successfully';
END
ELSE
BEGIN
    PRINT 'Conversations table already exists';
END
GO

-- Create Messages table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
BEGIN
    CREATE TABLE Messages (
        Id NVARCHAR(100) PRIMARY KEY,
        ConversationId NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL CHECK (Role IN ('user', 'assistant')),
        Content NVARCHAR(MAX) NOT NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Data NVARCHAR(MAX) NULL, -- JSON serialized QueryResult
        ChartData NVARCHAR(MAX) NULL, -- JSON serialized ChartData
        CONSTRAINT FK_Messages_Conversations FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
    CREATE INDEX IX_Messages_Timestamp ON Messages(Timestamp ASC);
    
    PRINT 'Messages table created successfully';
END
ELSE
BEGIN
    PRINT 'Messages table already exists';
END
GO

-- Insert sample conversation for demo tenant
DECLARE @DemoTenantId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @ConversationId NVARCHAR(100) = CAST(DATEDIFF_BIG(MILLISECOND, '1970-01-01', GETUTCDATE()) AS NVARCHAR(50)) + '-sampleconv';

IF NOT EXISTS (SELECT * FROM Conversations WHERE TenantId = @DemoTenantId)
BEGIN
    -- Create sample conversation
    INSERT INTO Conversations (Id, Title, CreatedAt, UpdatedAt, IsPinned, TenantId)
    VALUES (
        @ConversationId,
        'Sample Conversation - Sales Analysis',
        DATEADD(DAY, -7, GETUTCDATE()),
        DATEADD(DAY, -7, GETUTCDATE()),
        0,
        @DemoTenantId
    );

    -- Add sample messages
    INSERT INTO Messages (Id, ConversationId, Role, Content, Timestamp, Data, ChartData)
    VALUES 
    (
        CAST(DATEDIFF_BIG(MILLISECOND, '1970-01-01', DATEADD(DAY, -7, GETUTCDATE())) AS NVARCHAR(50)) + '-msg1',
        @ConversationId,
        'user',
        'Show me top 10 customers by revenue',
        DATEADD(DAY, -7, GETUTCDATE()),
        NULL,
        NULL
    ),
    (
        CAST(DATEDIFF_BIG(MILLISECOND, '1970-01-01', DATEADD(MINUTE, 1, DATEADD(DAY, -7, GETUTCDATE()))) AS NVARCHAR(50)) + '-msg2',
        @ConversationId,
        'assistant',
        'Here are the top 10 customers by revenue:',
        DATEADD(MINUTE, 1, DATEADD(DAY, -7, GETUTCDATE())),
        '{"Columns":["CustomerName","TotalRevenue"],"Rows":[{"CustomerName":"Global Industries","TotalRevenue":225000},{"CustomerName":"Cloud Ventures","TotalRevenue":215000}],"RowCount":10}',
        NULL
    );

    PRINT 'Sample conversation created for demo tenant';
END
GO

PRINT 'Conversations schema setup complete';
