using MaIN.Infrastructure.Models;

namespace MaIN.Infrastructure.Repositories.Abstract;

public interface IChatRepository
{
    Task<IEnumerable<ChatDocument>> GetAllChats();
    Task<ChatDocument?> GetChatById(string? id);
    Task AddChat(ChatDocument chat);
    Task UpdateChat(string? id, ChatDocument chat);
    Task DeleteChat(string? id);
}