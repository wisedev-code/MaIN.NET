using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities.ProviderParams;

public static class ProviderParamsFactory
{
    public static IProviderInferenceParams Create(BackendType backend) => backend switch
    {
        BackendType.Self => new LocalInferenceParams(),
        BackendType.OpenAi => new OpenAiParams(),
        BackendType.DeepSeek => new DeepSeekParams(),
        BackendType.GroqCloud => new GroqCloudParams(),
        BackendType.Xai => new XaiParams(),
        BackendType.Gemini => new GeminiParams(),
        BackendType.Anthropic => new AnthropicParams(),
        BackendType.Ollama => new OllamaParams(),
        _ => new LocalInferenceParams()
    };
}
