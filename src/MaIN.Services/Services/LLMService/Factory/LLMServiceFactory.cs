using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services.Services.LLMService.Factory;

public class LLMServiceFactory(IServiceProvider serviceProvider) : ILLMServiceFactory
{
    public ILLMService CreateService(BackendType backendType)
    {
        return backendType switch
        {
            BackendType.OpenAi => new OpenAiService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IMemoryFactory>(),
                serviceProvider.GetRequiredService<IMemoryService>()),

            BackendType.Gemini => new GeminiService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IMemoryFactory>(),
                serviceProvider.GetRequiredService<IMemoryService>()),

            BackendType.DeepSeek => new DeepSeekService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IMemoryFactory>(),
                serviceProvider.GetRequiredService<IMemoryService>()),

            BackendType.GroqCloud => new GroqCloudService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IMemoryFactory>(),
                serviceProvider.GetRequiredService<IMemoryService>()),

            BackendType.Anthropic => new AnthropicService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>()),

            BackendType.Self => new LLMService(
                serviceProvider.GetRequiredService<MaINSettings>(),
                serviceProvider.GetRequiredService<INotificationService>(),
                serviceProvider.GetRequiredService<IMemoryService>(),
                serviceProvider.GetRequiredService<IMemoryFactory>()),

            _ => throw new ArgumentOutOfRangeException(nameof(backendType))
        };
    }
}