using MaIN.Domain.Entities;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services;

public interface ILLMService
{
    Task<ChatResult?> Send(Chat? chat, bool removeSession = false, bool temporaryChat = false);
    Task<List<string>> GetCurrentModels();
}