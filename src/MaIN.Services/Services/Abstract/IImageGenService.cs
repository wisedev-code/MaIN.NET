using MaIN.Domain.Entities;
using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;

namespace MaIN.Services.Services.Abstract;

public interface IImageGenService 
{
    Task<ChatResult?> Send(Chat? chat);
}