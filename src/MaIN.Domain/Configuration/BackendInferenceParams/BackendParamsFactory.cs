using MaIN.Domain.Entities;

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
        BackendType.Vertex => new VertexInferenceParams(),
        _ => new LocalInferenceParams()
    };
}
