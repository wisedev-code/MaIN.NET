using System.Data;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlChatRepository : IChatRepository
{
    private readonly IDbConnection _connection;

    public SqlChatRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<ChatDocument>> GetAllChats() =>
        await _connection.QueryAsync<ChatDocument>(@"
            SELECT * FROM Chats");

    public async Task<ChatDocument> GetChatById(string id) =>
        await _connection.QueryFirstOrDefaultAsync<ChatDocument>(@"
            SELECT * FROM Chats 
            WHERE Id = @Id", new { Id = id });

    public async Task AddChat(ChatDocument chat)
    {
        var sql = @"
            INSERT INTO Chats (Id, CreatedAt, UpdatedAt) 
            VALUES (@Id, @CreatedAt, @UpdatedAt)";
            
        await _connection.ExecuteAsync(sql, chat);
    }

    public async Task UpdateChat(string id, ChatDocument chat)
    {
        var sql = @"
            UPDATE Chats 
            SET UpdatedAt = @UpdatedAt 
            WHERE Id = @Id";
            
        await _connection.ExecuteAsync(sql, chat);
    }

    public async Task DeleteChat(string id) =>
        await _connection.ExecuteAsync(@"
            DELETE FROM Chats 
            WHERE Id = @Id", new { Id = id });
}