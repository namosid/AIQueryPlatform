namespace AIQueryPlatform.Api.Services.Interfaces;

public interface ISqlValidatorService
{
    bool IsValidSelectQuery(string sql, out string? errorMessage);
    string EnforceRowLimit(string sql, int maxRows);
}
