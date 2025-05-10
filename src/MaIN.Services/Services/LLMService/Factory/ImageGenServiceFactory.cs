using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services.Services.LLMService.Factory;

public class ImageGenServiceFactory(IServiceProvider serviceProvider) : IImageGenServiceFactory
{
    public IImageGenService CreateService(BackendType backendType)
    {
        return backendType switch
        {
            BackendType.OpenAi => new OpenAiImageGenService(serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<MaINSettings>()),
            BackendType.Gemini => new GeminiImageGenService(serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<MaINSettings>()),
            BackendType.Self => new ImageGenService(serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<MaINSettings>()),
            
            // Add other backends as needed
            _ => throw new ArgumentOutOfRangeException(nameof(backendType))
        };
    }
}