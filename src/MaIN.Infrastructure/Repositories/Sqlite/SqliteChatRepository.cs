using Dapper;
using MaIN.Domain.Entities;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using System.Data;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.Sqlite;

public class SqliteChatRepository(IDbConnection connection) : IChatRepository
{
    private readonly JsonSerializerOptions? _jsonOptions = new()
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
            Messages = row.Messages is not null
                ? JsonSerializer.Deserialize<List<MessageDocument>>(row.Messages, _jsonOptions)
                : new List<MessageDocument>(),
            Type = row.Type is not null
                ? JsonSerializer.Deserialize<ChatTypeDocument>(row.Type, _jsonOptions)
                : default,
            ConvState = row.ConvState is not null
                ? JsonSerializer.Deserialize<dynamic>(row.ConvState, _jsonOptions)
                : default,
            InferenceParams = row.InferenceParams is not null
                ? JsonSerializer.Deserialize<InferenceParamsDocument>(row.InferenceParams, _jsonOptions)
                : default,
            MemoryParams = row.MemoryParams is not null
                ? JsonSerializer.Deserialize<MemoryParamsDocument>(row.MemoryParams, _jsonOptions)
                : default,
            Properties = row.Properties is not null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(row.Properties, _jsonOptions)
                : new Dictionary<string, string>(),
            ImageGen = Convert.ToBoolean(row.Visual),
            Interactive = Convert.ToBoolean(row.Interactive)
        };
        return chat;
    }

    private object MapChatToParameters(ChatDocument chat) =>
        new
        {
            chat.Id,
            chat.Name,
            chat.Model,
            Messages = JsonSerializer.Serialize(chat.Messages, _jsonOptions),
            ConvState = JsonSerializer.Serialize(chat.ConvState, _jsonOptions),
            InferenceParams = JsonSerializer.Serialize(chat.InferenceParams, _jsonOptions),
            MemoryParams = JsonSerializer.Serialize(chat.MemoryParams, _jsonOptions),
            Type = JsonSerializer.Serialize(chat.Type, _jsonOptions),
            Properties = JsonSerializer.Serialize(chat.Properties, _jsonOptions),
            Visual = chat.ImageGen ? 1 : 0,
            Interactive = chat.Interactive ? 1 : 0
        };

    public async Task<IEnumerable<Chat>> GetAllChats()
    {
        var rows = await connection.QueryAsync(
            "SELECT * FROM Chats");
        return rows.Select(MapChatDocument).Select(x => x.ToDomain());
    }

    public async Task<Chat?> GetChatById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM Chats WHERE Id = @Id",
            new { Id = id });
        return row is not null ? MapChatDocument(row).ToDomain() : null;
    }

    public async Task AddChat(Chat chat)
    {
        var parameters = MapChatToParameters(chat.ToDocument());
        await connection.ExecuteAsync(@"
            INSERT INTO Chats (Id, Name, Model, Messages, [Type], Properties, Visual, ConvState, InferenceParams, MemoryParams, Interactive) VALUES (@Id, @Name, @Model, @Messages, @Type, @Properties,
                 @Visual, @ConvState, @InferenceParams, @MemoryParams, @Interactive)", parameters);
    }

    public async Task UpdateChat(string id, Chat chat)
    {
        var parameters = MapChatToParameters(chat.ToDocument());
        await connection.ExecuteAsync(@"
            UPDATE Chats
            SET Name = @Name,
                Model = @Model,
                Messages = @Messages,
                Type = @Type,
                Properties = @Properties,
                ConvState = @ConvState,
                Visual = @Visual
            WHERE Id = @Id", parameters);
    }

    public async Task DeleteChat(string id) =>
        await connection.ExecuteAsync(
            "DELETE FROM Chats WHERE Id = @Id",
            new { Id = id });
}
