using MaIN.Domain.Configuration;

namespace MaIN.Domain;

public static class Extensions
{
    public static string GetApiKeyVariable(this BackendType backendType)
    {
        return backendType switch
        {
            BackendType.Self => "",
            BackendType.Anthropic => "ANTHROPIC_API_KEY",
            BackendType.DeepSeek => "DEEPSEEK_API_KEY",
            BackendType.Gemini => "GEMINI_API_KEY",
            BackendType.GroqCloud => "GROQ_API_KEY",
            BackendType.Ollama => "OLLAMA_API_KEY",
            BackendType.OpenAi => "OPENAI_API_KEY",
            BackendType.Xai => "XAI_API_KEY",
            _ => throw new ArgumentOutOfRangeException(nameof(BackendType))
        };
    }
}