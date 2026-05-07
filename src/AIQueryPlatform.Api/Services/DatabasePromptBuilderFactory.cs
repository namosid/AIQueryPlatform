using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services.Interfaces;
using AIQueryPlatform.Api.Services.PromptBuilders;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Factory for creating database-specific prompt builders
/// </summary>
public class DatabasePromptBuilderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DatabasePromptBuilderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDatabasePromptBuilder GetPromptBuilder(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => _serviceProvider.GetRequiredService<SqlServerPromptBuilder>(),
            DatabaseType.MySql => _serviceProvider.GetRequiredService<MySqlPromptBuilder>(),
            DatabaseType.PostgreSql => _serviceProvider.GetRequiredService<PostgreSqlPromptBuilder>(),
            DatabaseType.Excel => _serviceProvider.GetRequiredService<ExcelPromptBuilder>(),
            DatabaseType.Sqlite => _serviceProvider.GetRequiredService<SqlServerPromptBuilder>(), // SQLite uses similar syntax to SQL Server for basic queries
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }
}
