using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Domain.Repositories;
using System.Collections.Concurrent;

namespace MaIN.Infrastructure.Repositories;

public class DefaultChatRepository : IChatRepository
{
    private readonly ConcurrentDictionary<string, Chat> _chats = new();

    public Task<IEnumerable<Chat>> GetAllChats() => Task.FromResult(_chats.Values.AsEnumerable());

    public Task<Chat?> GetChatById(string id) => Task.FromResult(_chats.GetValueOrDefault(id));

    public Task AddChat(Chat chat) =>
        !_chats.TryAdd(chat.Id, chat)
            ? throw new ChatAlreadyExistsException(chat.Id)
            : Task.CompletedTask;

    public Task UpdateChat(string id, Chat chat) =>
        !_chats.TryUpdate(id, chat, _chats.GetValueOrDefault(id)!)
            ? throw new KeyNotFoundException($"Chat with ID {id} not found.")
            : Task.CompletedTask;
    public Task DeleteChat(string id) =>
        !_chats.TryRemove(id, out _)
            ? throw new KeyNotFoundException($"Chat with ID {id} not found.")
            : Task.CompletedTask;
}
