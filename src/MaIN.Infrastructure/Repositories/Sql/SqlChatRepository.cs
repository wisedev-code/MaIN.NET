using System.Data;
using System.Text.Json;
using Dapper;
using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlChatRepository(IDbConnection connection) : IChatRepository
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
                JsonSerializer.Deserialize<List<MessageDocument>>(row.Messages.ToString(), _jsonOptions) : 
                new List<MessageDocument>(),
            Type = row.Type != null ? 
                JsonSerializer.Deserialize<ChatTypeDocument>(row.Type.ToString(), _jsonOptions) : 
                default,
            InferenceParams = row.InferenceParams != null ? 
                JsonSerializer.Deserialize<InferenceParamsDocument>(row.InferenceParams.ToString(), _jsonOptions) : 
                default,
            Properties = row.Properties != null ? 
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Properties.ToString(), _jsonOptions) : 
                new Dictionary<string, string>(),
            Visual = row.Visual,
            Interactive = row.Interactive
        };
        return chat;
    }

    private object MapChatToParameters(ChatDocument chat)
    {
        if (chat == null)
            throw new ArgumentNullException(nameof(chat));

        return new
        {
            chat.Id,
            chat.Name,
            chat.Model,
            Messages = JsonSerializer.Serialize(chat.Messages ?? new List<MessageDocument>(), _jsonOptions),
            Type = JsonSerializer.Serialize(chat.Type, _jsonOptions),
            InferenceParams = JsonSerializer.Serialize(chat.InferenceParams, _jsonOptions),
            Properties = chat.Properties != null ? 
                JsonSerializer.Serialize(chat.Properties, _jsonOptions) : null,
            chat.Visual,
            chat.Interactive
        };
    }

    public async Task<IEnumerable<ChatDocument>> GetAllChats()
    {
        var rows = await connection.QueryAsync(@"
            SELECT * FROM Chats");
        return rows.Select(MapChatDocument);
    }

    public async Task<ChatDocument> GetChatById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(@"
            SELECT * FROM Chats 
            WHERE Id = @Id", new { Id = id });
        return row != null ? MapChatDocument(row) : null;
    }

    public async Task AddChat(ChatDocument chat)
    {
        if (chat == null)
            throw new ArgumentNullException(nameof(chat));

        var parameters = MapChatToParameters(chat);
        await connection.ExecuteAsync(@"
            INSERT INTO Chats (
                Id, Name, Model, Messages, Type, Properties, 
                Stream, Visual, InferenceParams, Interactive
            ) VALUES (
                @Id, @Name, @Model, @Messages, @Type, @Properties, 
                 @Visual, @InferenceParams, @Interactive)", 
            parameters);
    }

    public async Task UpdateChat(string id, ChatDocument chat)
    {
        if (chat == null)
            throw new ArgumentNullException(nameof(chat));

        var parameters = MapChatToParameters(chat);
        await connection.ExecuteAsync(@"
            UPDATE Chats 
            SET Name = @Name,
                Model = @Model,
                Messages = @Messages,
                Type = @Type,
                Properties = @Properties,
                Visual = @Visual
            WHERE Id = @Id",
            parameters);
    }

    public async Task DeleteChat(string id) =>
        await connection.ExecuteAsync(@"
            DELETE FROM Chats 
            WHERE Id = @Id",
            new { Id = id });

    // Optional: Add methods for JSON-specific queries
    public async Task<IEnumerable<ChatDocument>> GetChatsByProperty(string key, string value)
    {
        var rows = await connection.QueryAsync(@"
            SELECT *
            FROM Chats
            WHERE JSON_VALUE(Properties, '$." + key + @"') = @value",
            new { value });
        return rows.Select(MapChatDocument);
    }
}