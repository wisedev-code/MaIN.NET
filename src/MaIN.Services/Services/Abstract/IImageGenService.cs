using MaIN.Domain.Entities;
using MaIN.Services.Models;

namespace MaIN.Services.Services.Abstract;

public interface IImageGenService 
{
    Task<ChatResult?> Send(Chat chat);
}