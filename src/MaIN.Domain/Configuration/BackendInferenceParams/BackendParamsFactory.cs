using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Configuration.BackendInferenceParams;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public static class BackendParamsFactory
{
    public static IBackendInferenceParams Create(BackendType backend) => backend switch
    {
        BackendType.Self => new LocalInferenceParams(),
        BackendType.OpenAi => new OpenAiInferenceParams(),
        BackendType.DeepSeek => new DeepSeekInferenceParams(),
        BackendType.GroqCloud => new GroqCloudInferenceParams(),
        BackendType.Xai => new XaiInferenceParams(),
        BackendType.Gemini => new GeminiInferenceParams(),
        BackendType.Anthropic => new AnthropicInferenceParams(),
        BackendType.Ollama => new OllamaInferenceParams(),
        _ => new LocalInferenceParams()
    };
}
