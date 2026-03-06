using MaIN.Domain.Entities;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories.Mongo;

public class MongoChatRepository(IMongoDatabase database, string collectionName) : IChatRepository
{
    private readonly IMongoCollection<ChatDocument> _chats = database.GetCollection<ChatDocument>(collectionName);

    public async Task<IEnumerable<Chat>> GetAllChats() =>
        (await _chats.Find(chat => true).ToListAsync()).Select(d => d.ToDomain());

    public async Task<Chat?> GetChatById(string id) =>
        (await _chats.Find<ChatDocument>(chat => chat.Id == id).FirstOrDefaultAsync())?.ToDomain();

    public async Task AddChat(Chat chat) =>
        await _chats.InsertOneAsync(chat.ToDocument());

    public async Task UpdateChat(string id, Chat chat) =>
        await _chats.ReplaceOneAsync(x => x.Id == id, chat.ToDocument());

    public async Task DeleteChat(string id) =>
        await _chats.DeleteOneAsync(x => x.Id == id);
}
