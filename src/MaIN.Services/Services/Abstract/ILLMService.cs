using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Abstract;

public interface ILLMService
{
    Task<ChatResult?> Send(Chat chat,
        ChatRequestOptions requestOptions, 
        CancellationToken cancellationToken = default);
    Task<ChatResult?> AskMemory(Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default);
    Task<string[]> GetCurrentModels();
    Task CleanSessionCache(string id);
}