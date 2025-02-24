using MaIN.Domain.Entities;
using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services.Abstract;

public interface IImageGenService //TODO generalize with IOllamaService
{
    Task<ChatResult?> Send(Chat chat);
    Task<List<string>> GetCurrentModels();
}