using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services.Services.LLMService.Factory;

public class LLMServiceFactory(IServiceProvider serviceProvider) : ILLMServiceFactory
{
    public ILLMService CreateService(BackendType backendType)
    {
        // ActivatorUtilities.CreateInstance resolves every constructor param (including the
        // optional ILogger<T> overload) from the DI container — manual `new T(...)` calls
        // silently leave logger=null which makes every logger?.LogInformation a no-op.
        return backendType switch
        {
            BackendType.OpenAi => ActivatorUtilities.CreateInstance<OpenAiService>(serviceProvider),
            BackendType.Gemini => ActivatorUtilities.CreateInstance<GeminiService>(serviceProvider),
            BackendType.DeepSeek => ActivatorUtilities.CreateInstance<DeepSeekService>(serviceProvider),
            BackendType.GroqCloud => ActivatorUtilities.CreateInstance<GroqCloudService>(serviceProvider),
            BackendType.Xai => ActivatorUtilities.CreateInstance<XaiService>(serviceProvider),
            BackendType.Ollama => ActivatorUtilities.CreateInstance<OllamaService>(serviceProvider),
            BackendType.Anthropic => ActivatorUtilities.CreateInstance<AnthropicService>(serviceProvider),
            BackendType.Vertex => ActivatorUtilities.CreateInstance<VertexService>(serviceProvider),
            BackendType.Self => ActivatorUtilities.CreateInstance<LLMService>(serviceProvider),
            _ => throw new ArgumentOutOfRangeException(nameof(backendType))
        };
    }
}