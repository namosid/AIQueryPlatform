using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for managing saved analyses
/// </summary>
public class SavedAnalysisService : ISavedAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SavedAnalysisService> _logger;

    public SavedAnalysisService(
        IConfiguration configuration,
        ILogger<SavedAnalysisService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SavedAnalysisListResponse> ListSavedAnalysesAsync(Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get total count
            var countQuery = @"
                SELECT COUNT(*) 
                FROM SavedAnalyses 
                WHERE TenantId = @TenantId";

            int total;
            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                countCommand.Parameters.AddWithValue("@TenantId", tenantId);
                total = (int)await countCommand.ExecuteScalarAsync();
            }

            // Get saved analyses
            var query = @"
                SELECT sa.Id, sa.Title, sa.Description, sa.ConversationId, sa.TenantId, sa.SavedAt,
                       c.Title as ConversationTitle
                FROM SavedAnalyses sa
                LEFT JOIN Conversations c ON sa.ConversationId = c.Id
                WHERE sa.TenantId = @TenantId
                ORDER BY sa.SavedAt DESC";

            var analyses = new List<SavedAnalysisDto>();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var analysis = new SavedAnalysisDto
                    {
                        Id = reader.GetString(0),
                        Title = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ConversationId = reader.GetString(3),
                        TenantId = reader.GetGuid(4),
                        SavedAt = reader.GetDateTime(5)
                    };
                    analyses.Add(analysis);
                }
            }

            return new SavedAnalysisListResponse
            {
                Analyses = analyses,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing saved analyses for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<SavedAnalysisDto?> GetSavedAnalysisAsync(string id, Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT Id, Title, Description, ConversationId, TenantId, SavedAt
                FROM SavedAnalyses
                WHERE Id = @Id AND TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SavedAnalysisDto
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ConversationId = reader.GetString(3),
                    TenantId = reader.GetGuid(4),
                    SavedAt = reader.GetDateTime(5)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved analysis {Id} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    public async Task<SavedAnalysisDto> SaveAnalysisAsync(string conversationId, Guid tenantId, string title, string? description)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Verify conversation exists and belongs to tenant
            var checkQuery = @"
                SELECT COUNT(*) 
                FROM Conversations 
                WHERE Id = @Id AND TenantId = @TenantId";

            using (var checkCommand = new SqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@Id", conversationId);
                checkCommand.Parameters.AddWithValue("@TenantId", tenantId);
                var count = (int)await checkCommand.ExecuteScalarAsync();
                if (count == 0)
                {
                    throw new InvalidOperationException($"Conversation {conversationId} not found for tenant {tenantId}");
                }
            }

            var analysisId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N").Substring(0, 9)}";
            var savedAt = DateTime.UtcNow;

            var insertQuery = @"
                INSERT INTO SavedAnalyses (Id, Title, Description, ConversationId, TenantId, SavedAt)
                VALUES (@Id, @Title, @Description, @ConversationId, @TenantId, @SavedAt)";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", analysisId);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
                command.Parameters.AddWithValue("@ConversationId", conversationId);
                command.Parameters.AddWithValue("@TenantId", tenantId);
                command.Parameters.AddWithValue("@SavedAt", savedAt);

                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Saved analysis {AnalysisId} for conversation {ConversationId}", analysisId, conversationId);

            return new SavedAnalysisDto
            {
                Id = analysisId,
                Title = title,
                Description = description,
                ConversationId = conversationId,
                TenantId = tenantId,
                SavedAt = savedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving analysis for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> DeleteSavedAnalysisAsync(string id, Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var deleteQuery = @"
                DELETE FROM SavedAnalyses 
                WHERE Id = @Id AND TenantId = @TenantId";

            using var command = new SqlCommand(deleteQuery, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Deleted saved analysis {AnalysisId} for tenant {TenantId}", id, tenantId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved analysis {Id} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }
}
