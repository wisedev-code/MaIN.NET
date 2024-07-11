using MaIN.Models;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services.Abstract;

public interface IRagService
{
    Task<Chat> Completions(Chat chat, bool translatePrompt = false);
}