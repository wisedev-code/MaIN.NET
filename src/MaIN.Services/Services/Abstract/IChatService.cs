using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services.Abstract;

public interface IChatService
{
    Task Create(Chat chat);
    Task<ChatOllamaResult> Completions(Chat chat);
    Task Delete(string id);
    Task<Chat> GetById(string id);
    Task<List<string>> GetCurrentModels();
    Task<List<Chat>> GetAll();
}