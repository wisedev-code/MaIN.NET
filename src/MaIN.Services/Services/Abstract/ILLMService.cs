using MaIN.Domain.Entities;
using MaIN.Services.Models;

namespace MaIN.Services;

public interface ILLMService
{
    Task<ChatResult?> Send(Chat? chat, bool interactiveUpdates = false, bool createSession = false);
    Task<ChatResult?> AskMemory(Chat? chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        List<string>? webUrls = null,
        List<string>? memory = null);
    Task<List<string>> GetCurrentModels();
    Task CleanSessionCache(string id);
}