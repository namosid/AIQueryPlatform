# Multi-Database Support

## Overview

The AI Query Platform now supports multiple database types per tenant, allowing each tenant to use different database systems based on their needs.

## Supported Database Types

1. **SQL Server** (Default) - Microsoft SQL Server
2. **MySQL** - MySQL database
3. **PostgreSQL** - PostgreSQL database  
4. **Excel** - Excel files via OleDb (Windows only)
5. **SQLite** - Lightweight file-based database

## Architecture

### Components

1. **DatabaseType Enum** - Defines supported database types
2. **Database Prompt Builders** - Generate database-specific SQL syntax and system prompts
3. **Database Executors** - Execute queries against specific database types
4. **Tenant Configuration** - Each tenant can specify their database type

### How It Works

1. Tenant configuration includes `DatabaseType` field
2. When a natural language query is received:
   - The appropriate **Prompt Builder** generates database-specific system prompts for the AI
   - AI generates SQL compatible with the target database
   - The **Database Executor** executes the query using the appropriate database driver

## Database-Specific Features

### SQL Server
- Uses `TOP N` for limiting results
- Date functions: `GETDATE()`, `DATEADD()`, `DATEDIFF()`
- String concatenation: `+` operator or `CONCAT()`
- Supports CTEs and window functions

### MySQL
- Uses `LIMIT N` for limiting results
- Date functions: `NOW()`, `CURDATE()`, `DATE_ADD()`, `DATE_SUB()`
- String concatenation: `CONCAT()` function
- Supports CTEs (MySQL 8.0+)
- Case-insensitive by default
- Uses backticks for identifiers: \`table_name\`

### PostgreSQL
- Uses `LIMIT N` and `OFFSET` for pagination
- Date functions: `NOW()`, `CURRENT_DATE`, `date_trunc()`, `INTERVAL`
- String concatenation: `||` operator or `CONCAT()`
- Supports advanced features like arrays, JSON, and full-text search
- Case-sensitive for identifiers
- Uses double quotes for exact case: "TableName"

### Excel (OleDb)
- References sheets as `[SheetName$]` with square brackets and $ suffix
- Uses `TOP N` for limiting (like SQL Server)
- Limited function support
- No CTEs or window functions
- Column names with spaces: `[Column Name]`
- Windows only
- Best for small datasets

## Configuration

### Tenant Setup

#### 1. Update Database Schema

Run the updated `setup.sql` script to add `DatabaseType` and `DatabaseSettings` columns to the Tenants table.

```sql
ALTER TABLE Tenants ADD DatabaseType INT NOT NULL DEFAULT 0;
ALTER TABLE Tenants ADD DatabaseSettings NVARCHAR(MAX) NULL;
```

#### 2. Configure Tenant

When creating or updating a tenant, specify the database type:

```csharp
var tenant = new Tenant
{
    Name = "MySQL Customer",
    ApiKey = "mysql_api_key_123",
    DatabaseType = DatabaseType.MySql,
    ConnectionString = "Server=localhost;Database=mydb;Uid=root;Pwd=password;",
    IsActive = true
};
```

### Connection Strings

#### SQL Server
```
Server=localhost\\SQLEXPRESS;Database=MyDatabase;Trusted_Connection=true;TrustServerCertificate=true;
```

#### MySQL
```
Server=localhost;Database=mydb;Uid=username;Pwd=password;Port=3306;
```

#### PostgreSQL
```
Host=localhost;Database=mydb;Username=postgres;Password=password;Port=5432;
```

#### Excel
```
Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\path\\to\\file.xlsx;Extended Properties="Excel 12.0 Xml;HDR=YES";
```

## API Usage

The API automatically uses the correct database type based on tenant configuration. No changes to API calls are required.

### Example Query Flow

1. Client sends natural language query with API key
2. System identifies tenant and retrieves `DatabaseType`
3. Appropriate prompt builder generates database-specific system prompt
4. AI generates SQL in correct dialect
5. Database executor runs query using appropriate driver
6. Results are returned to client

## Adding New Database Types

To add support for a new database type:

1. **Add to DatabaseType enum** (`Models/DatabaseType.cs`)
```csharp
public enum DatabaseType
{
    // ... existing types
    NewDatabase = 5
}
```

2. **Create Prompt Builder** (`Services/PromptBuilders/NewDatabasePromptBuilder.cs`)
```csharp
public class NewDatabasePromptBuilder : IDatabasePromptBuilder
{
    public string BuildSystemPrompt(DatabaseSchema schema) { ... }
    public string GetSyntaxRules() { ... }
    public string CleanSqlResponse(string sql) { ... }
}
```

3. **Create Executor** (`Services/Executors/NewDatabaseExecutor.cs`)
```csharp
public class NewDatabaseExecutor : IDatabaseExecutor
{
    public DatabaseType SupportedDatabaseType => DatabaseType.NewDatabase;
    public async Task<QueryResult> ExecuteQueryAsync(...) { ... }
}
```

4. **Register in Program.cs**
```csharp
builder.Services.AddScoped<NewDatabasePromptBuilder>();
builder.Services.AddScoped<IDatabaseExecutor, NewDatabaseExecutor>();
```

5. **Update Factory** (if needed for special handling)

## Testing

### Testing Different Database Types

1. Set up test databases for each type
2. Create test tenants with different `DatabaseType` values
3. Execute sample queries and verify:
   - SQL syntax is correct for the database
   - Query executes successfully
   - Results are returned in expected format

### Sample Test Queries

```plaintext
- "Show me top 10 customers by revenue"
- "What were sales last week?"
- "List products with price greater than $100"
- "Show order count by month this year"
```

## Troubleshooting

### Common Issues

1. **Missing Database Driver**
   - Ensure appropriate NuGet package is installed
   - MySQL: `MySql.Data`
   - PostgreSQL: `Npgsql`
   - Excel: `System.Data.OleDb` (Windows only)

2. **Connection String Errors**
   - Verify connection string format for database type
   - Check credentials and network connectivity
   - Ensure database server is running

3. **SQL Syntax Errors**
   - Check that prompt builder rules match database version
   - Verify AI is using correct syntax for database type
   - Review generated SQL in logs

4. **Excel-Specific Issues**
   - Excel support is Windows-only
   - Requires Microsoft Access Database Engine
   - Sheet names must include `$` suffix
   - Limited to read-only operations

## Performance Considerations

- **Connection Pooling**: Enabled by default for SQL Server, MySQL, and PostgreSQL
- **Query Timeout**: Configurable per tenant (default: 30 seconds)
- **Result Limits**: Enforced to prevent large result sets (default: 100 rows)
- **Excel Files**: Best for small datasets (< 10,000 rows)

## Security

- Connection strings should be stored securely (use Azure Key Vault in production)
- Each tenant has isolated database access
- SQL injection protection through parameterized queries
- Query validation before execution
- Only SELECT queries are allowed

## Migration Guide

### From Single Database to Multi-Database

1. Run database migration script to add new columns
2. Update existing tenants with `DatabaseType = DatabaseType.SqlServer`
3. No code changes required for existing tenants
4. New tenants can specify different database types

### Database Schema Changes

The schema supports backward compatibility. Existing tenants without `DatabaseType` will default to SQL Server.

## Limitations

- Excel support is Windows-only
- Complex queries may have database-specific limitations
- Some advanced features (CTEs, window functions) may not be available in all database types
- AI may occasionally generate incorrect syntax - validation and error handling helps mitigate this

## Future Enhancements

- Support for MongoDB and other NoSQL databases
- Dynamic query optimization based on database capabilities
- Cross-database query federation
- Database migration tools
- Enhanced Excel support via EPPlus or ClosedXML
