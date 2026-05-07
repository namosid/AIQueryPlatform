# Advanced Sample Queries

This document contains complex, real-world query examples that demonstrate the power of the AI-Powered Query Platform. These queries showcase capabilities that typically require SQL expertise or custom report building in traditional software.

---

## 📋 Available Database Schema

The demo database includes the following tables:

### Core Business Tables
- **Customers**: CustomerId, CustomerName, Email, Phone, City, Country, TotalRevenue, CreatedAt
- **Products**: ProductId, ProductName, Category, Price, CostOfGoods, StockQuantity, ReorderLevel, IsActive
- **Orders**: OrderId, CustomerId, SalesRepId, OrderDate, TotalAmount, DiscountPercent, Status, ShippedDate, FulfilledByWarehouse, OrderSource
- **OrderItems**: OrderItemId, OrderId, ProductId, Quantity, UnitPrice, LineTotal

### Sales & Marketing
- **SalesRepresentatives**: SalesRepId, RepName, Email, Region, HireDate
- **MarketingCampaigns**: CampaignId, CampaignName, Channel, Budget, StartDate, EndDate, LeadsGenerated
- **CustomerAcquisition**: AcquisitionId, CustomerId, CampaignId, AcquisitionDate, AcquisitionCost

### Operations & Support
- **ProductReturns**: ReturnId, OrderItemId, ReturnDate, Reason, RefundAmount
- **SupportTickets**: TicketId, CustomerId, Subject, Status, Priority, CreatedAt, ResolvedAt

---

## 🎯 Sales & Revenue Intelligence

### 1. Month-over-Month Revenue Growth
```
Show me total revenue by month for the last 3 months with month-over-month growth percentage
```

### 2. Top Customers by Revenue
```
Show me the top 5 customers by total revenue, including their total order count and average order value
```

### 3. Sales Representative Performance
```
Show me total revenue and number of deals closed by each sales representative, ordered by revenue
```

### 4. High-Return Profitability Analysis
```
Find products that have returns but still maintain profitability above 40%, showing return count and profit margin
```

### 5. Discount Impact Analysis
```
Show me average order value grouped by discount percentage ranges (0%, 1-10%, 11-20%, 20%+) and sales representative
```

### 6. Product Category Revenue Breakdown
```
Show me total revenue and order count by product category for the last 60 days
```

---

## 📊 Operational Insights

### 6. Fulfillment Performance Analysis
```
Identify orders that took longer than average to fulfill in the last 30 days, and show which warehouse or fulfillment center was responsible
```

### 7. Inventory Stockout Impact
```
## 📊 Operational Insights

### 7. Fulfillment Performance by Warehouse
```
Show me average fulfillment time (days from order to shipment) by warehouse for orders in the last 60 days
```

### 8. Low Stock Alert
```
Find all products where current stock is below reorder level, showing product name, current stock, and reorder level
```

### 9. Order Status Distribution
```
Show me count of orders by status (Pending, Processing, Shipped, Completed) for the last 30 days
```

### 10. Order Source Performance
```
Compare total revenue and average order value across different order sources (Direct, Web, Partner)
```

---

## 👥 Customer Behavior Analytics

### 11. Cross-Product Purchase Analysis
```
Show me which products are most commonly purchased together in the same order
```

### 12. Repeat Customer Analysis
```
Identify customers who have placed more than one order, showing their total order count and lifetime value
```

### 13. Customer Support Patterns
```
Show me customers with open or in-progress support tickets, including ticket count and priority breakdown
```

### 14. Customer Acquisition Source Analysis
```
Show me total customers acquired by marketing campaign channel with average acquisition cost
```

### 15. High-Value Customer Identification
```
Find customers with total revenue above $20,000, showing their order count and average order value
```

---

## 💰 Financial & Profitability Analysis

### 16. Product Profitability by Category
```
Show me profit margin percentage by product category, calculated as (Price - CostOfGoods) / Price * 100
```

### 17. Most Profitable Products
```
Find the top 5 products by total profit, showing total revenue, total cost, and profit margin
```

### 18. Revenue Impact of Discounts
```
Calculate total revenue lost to discounts in the last 60 days, grouped by sales representative
```

### 19. Return Rate Impact on Profitability
```
Show me products with returns, calculating the net profit after deducting refund amounts from gross profit
```

### 20. Customer Acquisition ROI
```
Compare customer acquisition cost to total revenue generated per customer, grouped by marketing campaign
```

---

## 📈 Marketing & Customer Acquisition

### 21. Marketing Campaign Effectiveness
```
Show me all marketing campaigns with their budget, leads generated, and cost per lead
```

### 22. Lead Conversion Rate by Channel
```
Show me customers acquired by each marketing campaign channel with acquisition cost and total revenue generated
```

### 23. Campaign ROI Analysis
```
Calculate ROI for each marketing campaign by comparing total customer revenue to campaign budget
```

### 24. Acquisition Cost Trends
```
Show me average customer acquisition cost by marketing channel, comparing last month to previous months
```

---

## 🔮 Support & Customer Service Analytics

### 25. Support Ticket Volume Trends
```
Show me count of support tickets by status for the last 30 days
```

### 26. Average Ticket Resolution Time
```
Calculate average resolution time in days for resolved support tickets, grouped by priority level
```

### 27. Customers with Multiple Open Tickets
```
Find customers with 2 or more open support tickets, showing ticket count and oldest ticket date
```

### 28. Support Ticket Priority Distribution
```
Show me count and percentage of tickets by priority (High, Medium, Low)
```

---

## 🔄 Advanced Multi-Table Analysis

### 29. Complete Customer Revenue Breakdown
```
Show me customers with their total revenue, order count, product categories purchased, and support ticket count
```

### 30. Sales Rep Performance with Customer Details
```
Show me each sales representative with number of customers, total revenue, and average deal size
```

### 31. Product Performance Deep Dive
```
For each product, show total units sold, revenue generated, number of returns, and net profit after returns
```

### 32. Regional Performance Analysis
```
Show me total revenue, order count, and average order value grouped by customer country
```### 33. Order Fulfillment Analysis by Region
```
Show me average days to shipment for completed orders, grouped by sales representative region
```

---

## 🎯 Product & Inventory Intelligence

### 34. Best Selling Products
```
Show me top 10 products by total quantity sold across all orders
```

### 35. Low Turnover Inventory
```
Find products with stock quantity above 200 units that haven't appeared in any orders in the last 30 days
```

### 36. Category Performance Comparison
```
Compare total revenue, order count, and profit margin across all product categories
```

### 37. Discount vs Non-Discount Orders
```
Compare average order value between orders with discounts vs orders without discounts
```

---

## 📊 Time-Based Trend Analysis

### 38. Weekly Revenue Trends
```
Show me total revenue by week for the last 8 weeks
```

### 39. Order Volume by Day of Week
```
Show me order count and average order value grouped by day of week for the last 60 days
```

### 40. Recent vs Historical Performance
```
Compare total orders and revenue from last 30 days vs previous 30 days
```

### 41. Customer Growth Over Time
```
### 41. Customer Growth Over Time
```
Show me count of new customers by month for the last 6 months
```

---

## 🌟 Why These Queries Stand Out

### Traditional Software Limitations
- ❌ Most BI tools require pre-built reports or SQL knowledge
- ❌ Dashboard tools show fixed metrics, not ad-hoc complex analysis
- ❌ CRM/ERP systems have limited cross-functional query capabilities
- ❌ Excel/spreadsheets can't handle multi-table joins and complex logic
- ❌ Custom reports require IT/analytics team and long development cycles

### This Platform's Advantages
- ✅ **Natural Language** - No SQL knowledge required
- ✅ **Real-Time Analysis** - Instant results without waiting for reports
- ✅ **Complex Joins** - Automatically handles multi-table relationships
- ✅ **Ad-Hoc Questions** - Answer spontaneous business questions immediately
- ✅ **Cross-Functional Insights** - Combine data from sales, support, finance, etc.
- ✅ **Streaming Responses** - Large analytical queries with progress updates
- ✅ **Automatic Visualization** - Smart rendering as tables, charts, or PDFs

---

## 💡 Usage Tips

### Getting the Best Results

1. **Be Specific with Time Ranges**
   - ✅ "last 30 days" or "Q1 2026"
   - ❌ "recently"

2. **Define Your Metrics Clearly**
   - ✅ "revenue growth rate" or "customer lifetime value"
   - ❌ "how are we doing"

3. **Specify Segments or Filters**
   - ✅ "for enterprise customers" or "in the Northeast region"
   - ❌ "for some customers"

4. **Include Comparative Context**
   - ✅ "compared to last year" or "vs industry benchmark"
   - ❌ "show me sales"

5. **Request Specific Breakdowns**
   - ✅ "broken down by product category and region"
   - ❌ "give me a breakdown"

### Query Complexity Examples

**Simple Query:**
```
Show me total sales this month
```

**Intermediate Query:**
```
Show me top 10 customers by revenue in Q1 2026, including their order count and average order value
```

**Advanced Query:**
```
Show me each product with total units sold, revenue, cost of goods, profit margin percentage, return count, and net profit after returns, ordered by net profit descending
```

---

## 🚀 Getting Started

1. Open the frontend at `frontend/index.html`
2. Enter your tenant API key
3. Copy any query from this document
4. Paste it into the query box and click Execute
5. Watch as the AI converts your question to SQL and returns results

---

## 📝 Notes

- All queries respect your tenant's database schema and permissions
- Results are automatically limited to 100 rows for safety
- Complex queries may take a few seconds to process
- The AI will suggest the best visualization (table/chart/PDF) for each result
- All queries are logged for audit and optimization purposes

---

**Need more examples?** The platform can handle virtually any SELECT-based analytical query. Just ask your question in plain English!

---

## 🔧 Troubleshooting Common Issues

### SQL Syntax Errors

**Issue: "Incorrect syntax near ')'"**
- **Cause**: Unbalanced parentheses in complex calculations
- **Solution**: The system now validates parentheses automatically. Try rephrasing your query or check the generated SQL in logs.

**Issue: "Incorrect syntax near 'LIMIT'"**
- **Cause**: Wrong database syntax (MySQL/PostgreSQL instead of SQL Server)
- **Solution**: Fixed automatically - the system converts LIMIT to TOP for SQL Server.

**Issue: Complex window functions failing**
- **Cause**: Nested window functions or calculation errors
- **Solution**: Use CTE (WITH clause) to break complex queries into steps:
  ```sql
  WITH Step1 AS (SELECT ... GROUP BY ...),
       Step2 AS (SELECT ..., LAG(...) OVER (...) FROM Step1)
  SELECT * FROM Step2;
  ```

### Performance Issues

**Issue: Query taking too long**
- All queries are automatically limited to 100 rows
- Add specific date ranges to filter data: "in the last 30 days", "for Q1 2026"
- Use specific columns instead of "all fields"

### Chart Not Appearing

**Issue: Expected chart but got table**
- Charts appear for 2-50 rows with numeric data
- More than 50 rows automatically shows table (too cluttered for chart)
- Add TOP/LIMIT or date filters to reduce rows

**Issue: Multi-series chart not showing**
- Ensure query has one label column + multiple numeric columns
- Example: Month | Product A | Product B | Product C

### Getting Better Results

**✅ Good Queries:**
- "Show me monthly revenue for the last 12 months"
- "Compare top 5 products by sales across regions in Q1 2026"
- "Calculate customer lifetime value by acquisition channel"

**❌ Avoid:**
- "Show me everything" (too broad, no filters)
- "Give me sales" (missing time range and grouping)
- "All data from database" (not specific enough)

