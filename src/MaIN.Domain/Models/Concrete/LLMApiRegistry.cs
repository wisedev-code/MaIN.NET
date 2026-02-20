using MaIN.Domain.Configuration;

namespace MaIN.Domain.Models.Concrete;

public static class LLMApiRegistry
{
    public static readonly LLMApiRegistryEntry OpenAi = new("OpenAI", "OPENAI_API_KEY");
    public static readonly LLMApiRegistryEntry Gemini = new("Gemini", "GEMINI_API_KEY");
    public static readonly LLMApiRegistryEntry Deepseek = new("Deepseek", "DEEPSEEK_API_KEY");
    public static readonly LLMApiRegistryEntry Groq = new("GroqCloud", "GROQ_API_KEY");
    public static readonly LLMApiRegistryEntry Anthropic = new("Anthropic", "ANTHROPIC_API_KEY");
    public static readonly LLMApiRegistryEntry Xai = new("Xai", "XAI_API_KEY");
    public static readonly LLMApiRegistryEntry Ollama = new("Ollama", "OLLAMA_API_KEY");

    public static LLMApiRegistryEntry? GetEntry(BackendType backendType) => backendType switch
    {
        BackendType.OpenAi => OpenAi,
        BackendType.Gemini => Gemini,
        BackendType.DeepSeek => Deepseek,
        BackendType.GroqCloud => Groq,
        BackendType.Anthropic => Anthropic,
        BackendType.Xai => Xai,
        BackendType.Ollama => Ollama,
        _ => null
    };
}

public record LLMApiRegistryEntry(string ApiName, string ApiKeyEnvName);