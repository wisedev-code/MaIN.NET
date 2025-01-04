using System.Data;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sqlite;

public class SqliteChatRepository : IChatRepository
{
    private readonly IDbConnection _connection;

    public SqliteChatRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<ChatDocument>> GetAllChats() =>
        await _connection.QueryAsync<ChatDocument>("SELECT * FROM Chats");

    public async Task<ChatDocument> GetChatById(string id) =>
        await _connection.QueryFirstOrDefaultAsync<ChatDocument>(
            "SELECT * FROM Chats WHERE Id = @Id",
            new { Id = id });

    public async Task AddChat(ChatDocument chat) =>
        await _connection.ExecuteAsync(
            "INSERT INTO Chats (Id, /* other fields */) VALUES (@Id, /* other params */)",
            chat);

    public async Task UpdateChat(string id, ChatDocument chat) =>
        await _connection.ExecuteAsync(
            "UPDATE Chats SET /* fields = @values */ WHERE Id = @Id",
            chat);

    public async Task DeleteChat(string id) =>
        await _connection.ExecuteAsync(
            "DELETE FROM Chats WHERE Id = @Id",
            new { Id = id });
}