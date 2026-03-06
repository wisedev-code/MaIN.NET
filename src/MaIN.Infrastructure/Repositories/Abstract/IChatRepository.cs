using MaIN.Domain.Entities;

namespace MaIN.Infrastructure.Repositories.Abstract;

public interface IChatRepository
{
    Task<IEnumerable<Chat>> GetAllChats();
    Task<Chat?> GetChatById(string id);
    Task AddChat(Chat chat);
    Task UpdateChat(string id, Chat chat);
    Task DeleteChat(string id);
}
