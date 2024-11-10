using MaIN.Domain.Entities;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services;

public class LLMService : ILLMService
{
    public Task<ChatResult?> Send(Chat? chat)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetCurrentModels()
    {
        throw new NotImplementedException();
    }
}