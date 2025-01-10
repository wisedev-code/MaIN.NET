using System.Data;
using System.Text.Json;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

public class SqliteChatRepository(IDbConnection connection) : IChatRepository
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private ChatDocument MapChatDocument(dynamic row)
    {
        var chat = new ChatDocument
        {
            Id = row.Id,
            Name = row.Name,
            Model = row.Model,
            Messages = row.Messages != null ? 
                JsonSerializer.Deserialize<List<MessageDocument>>(row.Messages, _jsonOptions) : 
                new List<MessageDocument>(),
            Type = row.Type != null ? 
                JsonSerializer.Deserialize<ChatTypeDocument>(row.Type, _jsonOptions) : 
                default,
            Properties = row.Properties != null ? 
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Properties, _jsonOptions) : 
                new Dictionary<string, string>(),
            Stream = Convert.ToBoolean(row.Stream),
            Visual = Convert.ToBoolean(row.Visual)
        };
        return chat;
    }

    private object MapChatToParameters(ChatDocument chat)
    {
        return new
        {
            chat.Id,
            chat.Name,
            chat.Model,
            Messages = JsonSerializer.Serialize(chat.Messages, _jsonOptions),
            Type = JsonSerializer.Serialize(chat.Type, _jsonOptions),
            Properties = chat.Properties != null ? 
                JsonSerializer.Serialize(chat.Properties, _jsonOptions) : null,
            Stream = chat.Stream ? 1 : 0,
            Visual = chat.Visual ? 1 : 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    public async Task<IEnumerable<ChatDocument>> GetAllChats()
    {
        var rows = await connection.QueryAsync(
            "SELECT * FROM Chats");
        return rows.Select(MapChatDocument);
    }

    public async Task<ChatDocument> GetChatById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM Chats WHERE Id = @Id",
            new { Id = id });
        return row != null ? MapChatDocument(row) : null;
    }

    public async Task AddChat(ChatDocument chat)
    {
        var parameters = MapChatToParameters(chat);
        await connection.ExecuteAsync(@"
            INSERT INTO Chats (
                Id, Name, Model, Messages, Type, Properties, 
                Stream, Visual, CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @Name, @Model, @Messages, @Type, @Properties, 
                @Stream, @Visual, @CreatedAt, @UpdatedAt
            )", parameters);
    }

    public async Task UpdateChat(string id, ChatDocument chat)
    {
        var parameters = MapChatToParameters(chat);
        await connection.ExecuteAsync(@"
            UPDATE Chats 
            SET Name = @Name,
                Model = @Model,
                Messages = @Messages,
                Type = @Type,
                Properties = @Properties,
                Stream = @Stream,
                Visual = @Visual,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id", parameters);
    }

    public async Task DeleteChat(string id) =>
        await connection.ExecuteAsync(
            "DELETE FROM Chats WHERE Id = @Id",
            new { Id = id });
}