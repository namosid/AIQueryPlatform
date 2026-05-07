-- AI Query Platform Database Setup Script
-- This script creates the tenant management database

-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AIQueryPlatform')
BEGIN
    CREATE DATABASE AIQueryPlatform;
END
GO

USE AIQueryPlatform;
GO

-- Create Tenants table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
BEGIN
    CREATE TABLE Tenants (
        TenantId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(255) NOT NULL,
        ApiKey NVARCHAR(500) NOT NULL UNIQUE,
        ConnectionString NVARCHAR(1000) NOT NULL,
        LogoUrl NVARCHAR(500) NULL,
        ThemeColor NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );

    CREATE INDEX IX_Tenants_ApiKey ON Tenants(ApiKey);
    CREATE INDEX IX_Tenants_IsActive ON Tenants(IsActive);
END
GO

-- Insert sample tenants
IF NOT EXISTS (SELECT * FROM Tenants WHERE ApiKey = 'demo_api_key_12345')
BEGIN
    INSERT INTO Tenants (TenantId, Name, ApiKey, ConnectionString, LogoUrl, ThemeColor, IsActive)
    VALUES (
        '11111111-1111-1111-1111-111111111111',
        'Demo Tenant',
        'demo_api_key_12345',
        'Server=MWP336\SQLEXPRESS;Database=DemoTenantDB;Trusted_Connection=true;TrustServerCertificate=true;',
        'https://example.com/logo.png',
        '#0066CC',
        1
    );
END
ELSE
BEGIN
    -- Update existing tenant to point to correct database
    UPDATE Tenants 
    SET ConnectionString = 'Server=MWP336\SQLEXPRESS;Database=DemoTenantDB;Trusted_Connection=true;TrustServerCertificate=true;'
    WHERE ApiKey = 'demo_api_key_12345';
END
GO

-- Create sample tenant database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DemoTenantDB')
BEGIN
    CREATE DATABASE DemoTenantDB;
END
GO

    USE DemoTenantDB;
    GO

    -- Drop existing tables in correct order (respecting foreign keys)
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReturns')
        DROP TABLE ProductReturns;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerAcquisition')
        DROP TABLE CustomerAcquisition;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportTickets')
        DROP TABLE SupportTickets;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
        DROP TABLE OrderItems;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
        DROP TABLE Orders;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'MarketingCampaigns')
        DROP TABLE MarketingCampaigns;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesRepresentatives')
        DROP TABLE SalesRepresentatives;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
        DROP TABLE Products;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
        DROP TABLE Customers;
    GO

    -- Create sample tables for demo tenant

    -- Customers table
    CREATE TABLE Customers (
        CustomerId INT PRIMARY KEY IDENTITY(1,1),
        CustomerName NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255),
        Phone NVARCHAR(50),
        City NVARCHAR(100),
        Country NVARCHAR(100),
        TotalRevenue DECIMAL(18,2) DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );

    -- Insert sample data
    INSERT INTO Customers (CustomerName, Email, Phone, City, Country, TotalRevenue)
    VALUES 
        ('Acme Corporation', 'contact@acme.com', '555-0100', 'New York', 'USA', 150000.00),
        ('Global Industries', 'info@global.com', '555-0101', 'London', 'UK', 225000.00),
        ('Tech Solutions Inc', 'hello@techsol.com', '555-0102', 'San Francisco', 'USA', 180000.00),
        ('Innovation Labs', 'contact@innovate.com', '555-0103', 'Tokyo', 'Japan', 195000.00),
        ('Future Systems', 'info@future.com', '555-0104', 'Berlin', 'Germany', 165000.00),
        ('Digital Dynamics', 'sales@digital.com', '555-0105', 'Sydney', 'Australia', 142000.00),
        ('Smart Analytics', 'contact@smart.com', '555-0106', 'Toronto', 'Canada', 138000.00),
        ('Cloud Ventures', 'info@cloud.com', '555-0107', 'Singapore', 'Singapore', 215000.00),
        ('Data Corp', 'hello@data.com', '555-0108', 'Paris', 'France', 178000.00),
        ('AI Partners', 'contact@ai.com', '555-0109', 'Dubai', 'UAE', 205000.00);
    GO

    -- Products table
    CREATE TABLE Products (
        ProductId INT PRIMARY KEY IDENTITY(1,1),
        ProductName NVARCHAR(255) NOT NULL,
        Category NVARCHAR(100),
        Price DECIMAL(18,2),
        CostOfGoods DECIMAL(18,2),
        StockQuantity INT DEFAULT 0,
        ReorderLevel INT DEFAULT 10,
        IsActive BIT DEFAULT 1
    );

    -- Insert sample data with COGS for profitability analysis
    INSERT INTO Products (ProductName, Category, Price, CostOfGoods, StockQuantity, ReorderLevel)
    VALUES 
        ('Professional License', 'Software', 999.00, 150.00, 100, 20),
        ('Enterprise License', 'Software', 2499.00, 400.00, 50, 10),
        ('Cloud Storage 1TB', 'Cloud Services', 149.00, 80.00, 500, 50),
        ('Cloud Storage 5TB', 'Cloud Services', 499.00, 250.00, 200, 30),
        ('Premium Support', 'Services', 299.00, 100.00, 1000, 100),
        ('Training Package', 'Services', 1499.00, 600.00, 25, 5),
        ('API Access', 'Software', 399.00, 50.00, 150, 25),
        ('Mobile App License', 'Software', 199.00, 80.00, 300, 50),
        ('Consulting Hours', 'Services', 250.00, 150.00, 8, 2),
        ('Custom Development', 'Services', 5000.00, 2500.00, 5, 1);
    GO

    -- Sales Representatives table
    CREATE TABLE SalesRepresentatives (
        SalesRepId INT PRIMARY KEY IDENTITY(1,1),
        RepName NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255),
        Region NVARCHAR(100),
        HireDate DATETIME2 DEFAULT GETUTCDATE()
    );

    INSERT INTO SalesRepresentatives (RepName, Email, Region, HireDate)
    VALUES 
        ('John Smith', 'john.smith@company.com', 'North America', DATEADD(YEAR, -3, GETUTCDATE())),
        ('Sarah Johnson', 'sarah.j@company.com', 'Europe', DATEADD(YEAR, -2, GETUTCDATE())),
        ('Mike Chen', 'mike.chen@company.com', 'Asia Pacific', DATEADD(YEAR, -4, GETUTCDATE())),
        ('Emily Davis', 'emily.d@company.com', 'North America', DATEADD(YEAR, -1, GETUTCDATE())),
        ('Ahmed Hassan', 'ahmed.h@company.com', 'Middle East', DATEADD(MONTH, -8, GETUTCDATE()));
    GO

    -- Orders table
    CREATE TABLE Orders (
        OrderId INT PRIMARY KEY IDENTITY(1,1),
        CustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
        SalesRepId INT FOREIGN KEY REFERENCES SalesRepresentatives(SalesRepId) NULL,
        OrderDate DATETIME2 DEFAULT GETUTCDATE(),
        TotalAmount DECIMAL(18,2),
        DiscountPercent DECIMAL(5,2) DEFAULT 0,
        Status NVARCHAR(50) DEFAULT 'Pending',
        ShippedDate DATETIME2 NULL,
        FulfilledByWarehouse NVARCHAR(100) NULL,
        OrderSource NVARCHAR(50) DEFAULT 'Direct'
    );

    -- Insert sample data with varied dates for trend analysis
    INSERT INTO Orders (CustomerId, SalesRepId, OrderDate, TotalAmount, DiscountPercent, Status, ShippedDate, FulfilledByWarehouse, OrderSource)
    VALUES 
        -- Recent orders (last 30 days)
        (1, 1, DATEADD(DAY, -5, GETUTCDATE()), 15000.00, 5.0, 'Completed', DATEADD(DAY, -3, GETUTCDATE()), 'NY Warehouse', 'Direct'),
        (2, 2, DATEADD(DAY, -10, GETUTCDATE()), 22500.00, 0, 'Completed', DATEADD(DAY, -8, GETUTCDATE()), 'London Warehouse', 'Web'),
        (3, 1, DATEADD(DAY, -15, GETUTCDATE()), 18000.00, 10.0, 'Completed', DATEADD(DAY, -13, GETUTCDATE()), 'NY Warehouse', 'Partner'),
        (4, 3, DATEADD(DAY, -20, GETUTCDATE()), 19500.00, 0, 'Shipped', DATEADD(DAY, -18, GETUTCDATE()), 'Tokyo Warehouse', 'Direct'),
        (5, 4, DATEADD(DAY, -25, GETUTCDATE()), 16500.00, 15.0, 'Processing', NULL, NULL, 'Web'),
        
        -- Previous month
        (6, 1, DATEADD(DAY, -35, GETUTCDATE()), 14200.00, 5.0, 'Completed', DATEADD(DAY, -33, GETUTCDATE()), 'NY Warehouse', 'Direct'),
        (7, 2, DATEADD(DAY, -40, GETUTCDATE()), 13800.00, 0, 'Completed', DATEADD(DAY, -38, GETUTCDATE()), 'London Warehouse', 'Web'),
        (8, 3, DATEADD(DAY, -45, GETUTCDATE()), 21000.00, 20.0, 'Completed', DATEADD(DAY, -43, GETUTCDATE()), 'Tokyo Warehouse', 'Partner'),
        
        -- 2-3 months ago for trend analysis
        (1, 1, DATEADD(DAY, -65, GETUTCDATE()), 12500.00, 0, 'Completed', DATEADD(DAY, -63, GETUTCDATE()), 'NY Warehouse', 'Direct'),
        (2, 2, DATEADD(DAY, -70, GETUTCDATE()), 19800.00, 5.0, 'Completed', DATEADD(DAY, -68, GETUTCDATE()), 'London Warehouse', 'Web'),
        (4, 3, DATEADD(DAY, -80, GETUTCDATE()), 17200.00, 0, 'Completed', DATEADD(DAY, -78, GETUTCDATE()), 'Tokyo Warehouse', 'Direct'),
        (5, 4, DATEADD(DAY, -90, GETUTCDATE()), 15600.00, 10.0, 'Completed', DATEADD(DAY, -88, GETUTCDATE()), 'NY Warehouse', 'Web');
    GO

    -- OrderItems table
    CREATE TABLE OrderItems (
        OrderItemId INT PRIMARY KEY IDENTITY(1,1),
        OrderId INT FOREIGN KEY REFERENCES Orders(OrderId),
        ProductId INT FOREIGN KEY REFERENCES Products(ProductId),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL
    );

    -- Insert order line items
    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
    VALUES 
        (1, 1, 10, 999.00, 9990.00),
        (1, 5, 17, 299.00, 5083.00),
        (2, 2, 9, 2499.00, 22491.00),
        (3, 1, 15, 999.00, 14985.00),
        (3, 7, 8, 399.00, 3192.00),
        (4, 2, 8, 2499.00, 19992.00),
        (5, 3, 100, 149.00, 14900.00),
        (5, 5, 5, 299.00, 1495.00),
        (6, 1, 10, 999.00, 9990.00),
        (6, 8, 20, 199.00, 3980.00),
        (7, 5, 45, 299.00, 13455.00),
        (8, 2, 8, 2499.00, 19992.00),
        (8, 6, 1, 1499.00, 1499.00),
        (9, 1, 12, 999.00, 11988.00),
        (10, 4, 40, 499.00, 19960.00),
        (11, 2, 7, 2499.00, 17493.00),
        (12, 3, 100, 149.00, 14900.00);
    GO

    -- ProductReturns table
    CREATE TABLE ProductReturns (
        ReturnId INT PRIMARY KEY IDENTITY(1,1),
        OrderItemId INT FOREIGN KEY REFERENCES OrderItems(OrderItemId),
        ReturnDate DATETIME2 DEFAULT GETUTCDATE(),
        Reason NVARCHAR(255),
        RefundAmount DECIMAL(18,2)
    );

    -- Insert sample returns
    INSERT INTO ProductReturns (OrderItemId, ReturnDate, Reason, RefundAmount)
    VALUES 
        (1, DATEADD(DAY, -10, GETUTCDATE()), 'Defective product', 999.00),
        (5, DATEADD(DAY, -20, GETUTCDATE()), 'Not as described', 399.00),
        (9, DATEADD(DAY, -30, GETUTCDATE()), 'Changed mind', 1998.00);
    GO

    -- SupportTickets table
    CREATE TABLE SupportTickets (
        TicketId INT PRIMARY KEY IDENTITY(1,1),
        CustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
        Subject NVARCHAR(500),
        Status NVARCHAR(50) DEFAULT 'Open',
        Priority NVARCHAR(50) DEFAULT 'Medium',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        ResolvedAt DATETIME2 NULL
    );

    INSERT INTO SupportTickets (CustomerId, Subject, Status, Priority, CreatedAt, ResolvedAt)
    VALUES 
        (1, 'Installation issue', 'Resolved', 'High', DATEADD(DAY, -20, GETUTCDATE()), DATEADD(DAY, -18, GETUTCDATE())),
        (2, 'Billing question', 'Resolved', 'Medium', DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, -14, GETUTCDATE())),
        (3, 'Feature request', 'Open', 'Low', DATEADD(DAY, -10, GETUTCDATE()), NULL),
        (4, 'Login problem', 'In Progress', 'High', DATEADD(DAY, -5, GETUTCDATE()), NULL),
        (1, 'Documentation unclear', 'Resolved', 'Low', DATEADD(DAY, -25, GETUTCDATE()), DATEADD(DAY, -23, GETUTCDATE())),
        (5, 'Performance issue', 'Open', 'High', DATEADD(DAY, -3, GETUTCDATE()), NULL);
    GO

    -- MarketingCampaigns table
    CREATE TABLE MarketingCampaigns (
        CampaignId INT PRIMARY KEY IDENTITY(1,1),
        CampaignName NVARCHAR(255) NOT NULL,
        Channel NVARCHAR(100),
        Budget DECIMAL(18,2),
        StartDate DATETIME2,
        EndDate DATETIME2,
        LeadsGenerated INT DEFAULT 0
    );

    INSERT INTO MarketingCampaigns (CampaignName, Channel, Budget, StartDate, EndDate, LeadsGenerated)
    VALUES 
        ('Spring Sale 2026', 'Email', 15000.00, DATEADD(DAY, -90, GETUTCDATE()), DATEADD(DAY, -60, GETUTCDATE()), 150),
        ('Google Ads Q1', 'Paid Search', 25000.00, DATEADD(DAY, -120, GETUTCDATE()), DATEADD(DAY, -30, GETUTCDATE()), 320),
        ('LinkedIn Outreach', 'Social Media', 10000.00, DATEADD(DAY, -60, GETUTCDATE()), DATEADD(DAY, -30, GETUTCDATE()), 95),
        ('Webinar Series', 'Content', 8000.00, DATEADD(DAY, -45, GETUTCDATE()), DATEADD(DAY, -15, GETUTCDATE()), 180);
    GO

    -- CustomerAcquisition table
    CREATE TABLE CustomerAcquisition (
        AcquisitionId INT PRIMARY KEY IDENTITY(1,1),
        CustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
        CampaignId INT FOREIGN KEY REFERENCES MarketingCampaigns(CampaignId) NULL,
        AcquisitionDate DATETIME2 DEFAULT GETUTCDATE(),
        AcquisitionCost DECIMAL(18,2)
    );

    INSERT INTO CustomerAcquisition (CustomerId, CampaignId, AcquisitionDate, AcquisitionCost)
    VALUES 
        (1, 1, DATEADD(DAY, -80, GETUTCDATE()), 250.00),
        (2, 2, DATEADD(DAY, -100, GETUTCDATE()), 180.00),
        (3, 2, DATEADD(DAY, -95, GETUTCDATE()), 200.00),
        (4, 3, DATEADD(DAY, -50, GETUTCDATE()), 150.00),
        (5, 4, DATEADD(DAY, -40, GETUTCDATE()), 120.00),
        (6, 1, DATEADD(DAY, -75, GETUTCDATE()), 220.00),
        (7, 4, DATEADD(DAY, -35, GETUTCDATE()), 140.00),
        (8, 2, DATEADD(DAY, -90, GETUTCDATE()), 190.00);
    GO

    PRINT 'Database setup completed successfully!';
    PRINT 'Sample tenant created: Demo Tenant';
    PRINT 'API Key: demo_api_key_12345';
    PRINT 'Sample data inserted into DemoTenantDB';
    PRINT '';
    PRINT 'Tables created:';
    PRINT '  - Customers (10 records)';
    PRINT '  - Products (10 records with COGS)';
    PRINT '  - SalesRepresentatives (5 records)';
    PRINT '  - Orders (12 records with historical data)';
    PRINT '  - OrderItems (17 records)';
    PRINT '  - ProductReturns (3 records)';
    PRINT '  - SupportTickets (6 records)';
    PRINT '  - MarketingCampaigns (4 records)';
    PRINT '  - CustomerAcquisition (8 records)';
    PRINT '';
    PRINT 'Database now supports complex queries including:';
    PRINT '  - Revenue trends and growth analysis';
    PRINT '  - Return rate and profitability analysis';
    PRINT '  - Sales representative performance';
    PRINT '  - Marketing campaign effectiveness';
    PRINT '  - Customer acquisition cost analysis';
    PRINT '  - Support ticket analytics';
    PRINT '  - Order fulfillment performance';
