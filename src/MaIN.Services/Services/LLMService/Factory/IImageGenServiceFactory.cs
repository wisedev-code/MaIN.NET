using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services.LLMService.Factory;

public interface IImageGenServiceFactory
{
    IImageGenService? CreateService(BackendType backendType);
}