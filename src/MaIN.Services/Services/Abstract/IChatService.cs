using MaIN.Domain.Entities;
using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services.Abstract;

public interface IChatService
{
    Task Create(Chat? chat);
    Task<ChatResult> Completions(Chat? chat, bool translatePrompt = false, bool interactiveUpdates = false);
    Task Delete(string id);
    Task<Chat> GetById(string id);
    Task<List<Chat>> GetAll();
}