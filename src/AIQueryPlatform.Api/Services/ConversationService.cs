using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Service for managing conversations and messages
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConfiguration configuration,
        ILogger<ConversationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ConversationListResponse> ListConversationsAsync(Guid tenantId, int page = 1, int pageSize = 20)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get total count
            var countQuery = @"
                SELECT COUNT(*) 
                FROM Conversations 
                WHERE TenantId = @TenantId";

            int total;
            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                countCommand.Parameters.AddWithValue("@TenantId", tenantId);
                total = (int)await countCommand.ExecuteScalarAsync();
            }

            // Get conversations with pagination
            var query = @"
                SELECT Id, Title, CreatedAt, UpdatedAt, IsPinned, TenantId
                FROM Conversations
                WHERE TenantId = @TenantId
                ORDER BY IsPinned DESC, UpdatedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var conversations = new List<Conversation>();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TenantId", tenantId);
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var conversation = new Conversation
                    {
                        Id = reader.GetString(0),
                        Title = reader.GetString(1),
                        CreatedAt = reader.GetDateTime(2),
                        UpdatedAt = reader.GetDateTime(3),
                        IsPinned = reader.GetBoolean(4),
                        TenantId = reader.GetGuid(5),
                        Messages = new List<Message>() // Don't load messages in list view
                    };
                    conversations.Add(conversation);
                }
            }

            return new ConversationListResponse
            {
                Conversations = conversations,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversations for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId, Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get conversation
            var conversationQuery = @"
                SELECT Id, Title, CreatedAt, UpdatedAt, IsPinned, TenantId
                FROM Conversations
                WHERE Id = @Id AND TenantId = @TenantId";

            Conversation? conversation = null;

            using (var command = new SqlCommand(conversationQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", conversationId);
                command.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    conversation = new Conversation
                    {
                        Id = reader.GetString(0),
                        Title = reader.GetString(1),
                        CreatedAt = reader.GetDateTime(2),
                        UpdatedAt = reader.GetDateTime(3),
                        IsPinned = reader.GetBoolean(4),
                        TenantId = reader.GetGuid(5),
                        Messages = new List<Message>()
                    };
                }
            }

            if (conversation == null)
                return null;

            // Get messages for this conversation
            var messagesQuery = @"
                SELECT Id, ConversationId, Role, Content, Timestamp, Data, ChartData
                FROM Messages
                WHERE ConversationId = @ConversationId
                ORDER BY Timestamp ASC";

            using (var command = new SqlCommand(messagesQuery, connection))
            {
                command.Parameters.AddWithValue("@ConversationId", conversationId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var message = new Message
                    {
                        Id = reader.GetString(0),
                        ConversationId = reader.GetString(1),
                        Role = reader.GetString(2),
                        Content = reader.GetString(3),
                        Timestamp = reader.GetDateTime(4)
                    };

                    // Deserialize JSON data if present
                    if (!reader.IsDBNull(5))
                    {
                        var dataJson = reader.GetString(5);
                        message.Data = JsonSerializer.Deserialize<QueryResult>(dataJson);
                    }

                    if (!reader.IsDBNull(6))
                    {
                        var chartDataJson = reader.GetString(6);
                        message.ChartData = JsonSerializer.Deserialize<ChartData>(chartDataJson);
                    }

                    conversation.Messages.Add(message);
                }
            }

            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId} for tenant {TenantId}", conversationId, tenantId);
            throw;
        }
    }

    public async Task<Conversation> CreateConversationAsync(string? title, Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var conversationId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N").Substring(0, 9)}";
            var now = DateTime.UtcNow;
            var conversationTitle = title ?? "New Conversation";

            var query = @"
                INSERT INTO Conversations (Id, Title, CreatedAt, UpdatedAt, IsPinned, TenantId)
                VALUES (@Id, @Title, @CreatedAt, @UpdatedAt, @IsPinned, @TenantId)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", conversationId);
            command.Parameters.AddWithValue("@Title", conversationTitle);
            command.Parameters.AddWithValue("@CreatedAt", now);
            command.Parameters.AddWithValue("@UpdatedAt", now);
            command.Parameters.AddWithValue("@IsPinned", false);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Created conversation {ConversationId} for tenant {TenantId}", conversationId, tenantId);

            return new Conversation
            {
                Id = conversationId,
                Title = conversationTitle,
                CreatedAt = now,
                UpdatedAt = now,
                IsPinned = false,
                TenantId = tenantId,
                Messages = new List<Message>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Message> AddMessageAsync(string conversationId, Guid tenantId, AddMessageRequest request)
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

            var messageId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N").Substring(0, 9)}";
            var timestamp = DateTime.UtcNow;

            // Serialize data and chartData to JSON
            string? dataJson = request.Data != null ? JsonSerializer.Serialize(request.Data) : null;
            string? chartDataJson = request.ChartData != null ? JsonSerializer.Serialize(request.ChartData) : null;

            var insertQuery = @"
                INSERT INTO Messages (Id, ConversationId, Role, Content, Timestamp, Data, ChartData)
                VALUES (@Id, @ConversationId, @Role, @Content, @Timestamp, @Data, @ChartData)";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", messageId);
                command.Parameters.AddWithValue("@ConversationId", conversationId);
                command.Parameters.AddWithValue("@Role", request.Role);
                command.Parameters.AddWithValue("@Content", request.Content);
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.Parameters.AddWithValue("@Data", (object?)dataJson ?? DBNull.Value);
                command.Parameters.AddWithValue("@ChartData", (object?)chartDataJson ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }

            // Update conversation UpdatedAt
            var updateQuery = @"
                UPDATE Conversations 
                SET UpdatedAt = @UpdatedAt 
                WHERE Id = @Id";

            using (var command = new SqlCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@UpdatedAt", timestamp);
                command.Parameters.AddWithValue("@Id", conversationId);
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Added message {MessageId} to conversation {ConversationId}", messageId, conversationId);

            return new Message
            {
                Id = messageId,
                ConversationId = conversationId,
                Role = request.Role,
                Content = request.Content,
                Timestamp = timestamp,
                Data = request.Data,
                ChartData = request.ChartData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> UpdateConversationTitleAsync(string conversationId, Guid tenantId, string title)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var updateQuery = @"
                UPDATE Conversations 
                SET Title = @Title, UpdatedAt = @UpdatedAt
                WHERE Id = @Id AND TenantId = @TenantId";

            using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", conversationId);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Updated conversation {ConversationId} title to: {Title}", conversationId, title);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error updating conversation {ConversationId} title", conversationId);
            throw;
        }
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, Guid tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Delete messages first (foreign key constraint)
            var deleteMessagesQuery = @"
                DELETE FROM Messages 
                WHERE ConversationId = @ConversationId";

            using (var command = new SqlCommand(deleteMessagesQuery, connection))
            {
                command.Parameters.AddWithValue("@ConversationId", conversationId);
                await command.ExecuteNonQueryAsync();
            }

            // Delete conversation
            var deleteConversationQuery = @"
                DELETE FROM Conversations 
                WHERE Id = @Id AND TenantId = @TenantId";

            using (var command = new SqlCommand(deleteConversationQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", conversationId);
                command.Parameters.AddWithValue("@TenantId", tenantId);
                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Deleted conversation {ConversationId} for tenant {TenantId}", conversationId, tenantId);
                    return true;
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> PinConversationAsync(string conversationId, Guid tenantId, bool pinned)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Conversations 
                SET IsPinned = @IsPinned 
                WHERE Id = @Id AND TenantId = @TenantId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IsPinned", pinned);
            command.Parameters.AddWithValue("@Id", conversationId);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Updated pin status for conversation {ConversationId} to {Pinned}", conversationId, pinned);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning conversation {ConversationId}", conversationId);
            throw;
        }
    }
}
