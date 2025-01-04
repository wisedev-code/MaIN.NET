using System.Collections.Concurrent;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories;

public class DefaultChatRepository : IChatRepository
{
    private readonly ConcurrentDictionary<string, ChatDocument> _chats = new();

    public async Task<IEnumerable<ChatDocument>> GetAllChats() =>
        await Task.FromResult(_chats.Values);

    public async Task<ChatDocument> GetChatById(string id) =>
        await Task.FromResult(_chats.GetValueOrDefault(id));

    public async Task AddChat(ChatDocument chat)
    {
        if (!_chats.TryAdd(chat.Id, chat))
            throw new InvalidOperationException($"Chat with ID {chat.Id} already exists.");
            
        await Task.CompletedTask;
    }

    public async Task UpdateChat(string id, ChatDocument chat)
    {
        if (!_chats.TryUpdate(id, chat, _chats.GetValueOrDefault(id)))
            throw new KeyNotFoundException($"Chat with ID {id} not found.");
            
        await Task.CompletedTask;
    }

    public async Task DeleteChat(string id)
    {
        if (!_chats.TryRemove(id, out _))
            throw new KeyNotFoundException($"Chat with ID {id} not found.");
            
        await Task.CompletedTask;
    }
}