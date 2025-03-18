using MaIN.Domain.Entities;
using MaIN.Services.Dtos;

namespace MaIN.Services.Services.Abstract;

public interface IImageGenService 
{
    Task<ChatResult?> Send(Chat chat);
}