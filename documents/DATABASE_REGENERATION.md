# Database Regeneration Guide

## ⚠️ Important: Database Schema Updated

The sample database schema has been significantly enhanced to support the complex queries in SAMPLE_QUERIES.md.

### What Changed

**New Tables Added:**
- `SalesRepresentatives` - Sales team members with regions
- `OrderItems` - Detailed line items for each order  
- `ProductReturns` - Return tracking with reasons and refund amounts
- `SupportTickets` - Customer support ticket tracking
- `MarketingCampaigns` - Campaign tracking with budgets and leads
- `CustomerAcquisition` - Links customers to acquisition sources

**Enhanced Existing Tables:**
- `Orders` - Added SalesRepId, DiscountPercent, FulfilledByWarehouse, OrderSource
- `Products` - Added CostOfGoods for profitability calculations

### How to Regenerate the Database

#### Option 1: SQL Server Management Studio (SSMS)

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open `database/setup.sql` 
4. Execute the entire script (F5)
5. The script will:
   - Create AIQueryPlatform database (if not exists)
   - Create/update Tenants table
   - Create DemoTenantDB (if not exists)
   - Drop and recreate all tables with new schema
   - Insert sample data with relationships

#### Option 2: Command Line (sqlcmd)

```powershell
# From project root directory
sqlcmd -S localhost -d master -i database\setup.sql
```

#### Option 3: Visual Studio Code with SQL extension

1. Install "SQL Server (mssql)" extension
2. Connect to your SQL Server instance
3. Open `database/setup.sql`
4. Right-click and select "Execute Query"

### Verification

After running the script, you should see output like:

```
Database setup completed successfully!
Sample tenant created: Demo Tenant
API Key: demo_api_key_12345
Sample data inserted into DemoTenantDB

Tables created:
  - Customers (10 records)
  - Products (10 records with COGS)
  - SalesRepresentatives (5 records)
  - Orders (12 records with historical data)
  - OrderItems (17 records)
  - ProductReturns (3 records)
  - SupportTickets (6 records)
  - MarketingCampaigns (4 records)
  - CustomerAcquisition (8 records)
```

### Testing Sample Queries

After regenerating the database:

1. Start the API: `dotnet run --project src/AIQueryPlatform.Api`
2. Open `frontend/index.html` in a browser
3. Enter API Key: `demo_api_key_12345`
4. Try these test queries:

**Simple Test:**
```
Show me all customers
```

**Medium Test:**
```
Show me top 5 products by total revenue with order count
```

**Complex Test:**
```
Show me each sales representative with total revenue, order count, and average deal size
```

**Profitability Test:**
```
Find products with profit margin above 50% showing revenue and profit
```

### Troubleshooting

**Error: Database 'AIQueryPlatform' already exists**
- The script uses `IF NOT EXISTS` checks, so this is safe to ignore
- Tables will be recreated if they already exist

**Error: Cannot drop table because it's referenced by foreign key**
- The script includes proper DROP order
- If you get this error, you may need to manually drop all tables first

**Error: Connection failed**
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Ensure you have CREATE DATABASE permissions

### Sample Data Overview

- **10 Customers** across different countries
- **10 Products** in 2 categories (Software, Cloud Services, Services)  
- **5 Sales Representatives** covering different regions
- **12 Orders** spanning last 90 days for trend analysis
- **17 Order Line Items** linking orders to products
- **3 Product Returns** for return rate analysis
- **6 Support Tickets** with various statuses
- **4 Marketing Campaigns** across different channels
- **8 Customer Acquisitions** linked to campaigns

All data includes realistic timestamps for time-based trend analysis.

### Next Steps

Once the database is regenerated, all 41+ sample queries in SAMPLE_QUERIES.md should work correctly!
