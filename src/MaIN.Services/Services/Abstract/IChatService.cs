using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;

namespace MaIN.Services.Services.Abstract;

public interface IChatService
{
    Task Create(Chat chat);
    Task<ChatResult> Completions(
        Chat chat,
        bool translatePrompt = false,
        bool interactiveUpdates = false,
        Func<LLMTokenValue?, Task>? changeOfValue = null);
    Task Delete(string id);
    Task<Chat> GetById(string id);
    Task<List<Chat>> GetAll();
}