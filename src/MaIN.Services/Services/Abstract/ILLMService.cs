using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Abstract;

public interface ILLMService
{
    Task<ChatResult?> Send(Chat chat,
        bool interactiveUpdates = false,
        bool createSession = false,
        Func<LLMTokenValue, Task>? changeOfValue = null);
    Task<ChatResult?> AskMemory(Chat chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        Dictionary<string, FileStream>? streamData = null,
        List<string>? webUrls = null,
        List<string>? memory = null);
    Task<List<string?>> GetCurrentModels();
    Task CleanSessionCache(string id);
}