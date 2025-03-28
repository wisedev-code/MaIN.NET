using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories;

public class MongoChatRepository(IMongoDatabase database, string collectionName) : IChatRepository
{
    private readonly IMongoCollection<ChatDocument> _chats = database.GetCollection<ChatDocument>(collectionName);

    public async Task<IEnumerable<ChatDocument>> GetAllChats() =>
        await _chats.Find(chat => true).ToListAsync();

    public async Task<ChatDocument?> GetChatById(string id) =>
        await _chats.Find<ChatDocument>(chat => chat.Id == id).FirstOrDefaultAsync();

    public async Task AddChat(ChatDocument chat) =>
        await _chats.InsertOneAsync(chat);

    public async Task UpdateChat(string id, ChatDocument chat) =>
        await _chats.ReplaceOneAsync(x => x.Id == id, chat);
    
    public async Task DeleteChat(string id) =>
        await _chats.DeleteOneAsync(x => x.Id == id);
}