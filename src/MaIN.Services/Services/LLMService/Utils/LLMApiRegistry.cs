namespace MaIN.Services.Services.LLMService.Utils;

public static class LLMApiRegistry
{
    public static readonly LLMApiRegistryEntry OpenAi = new("OpenAI", "OPENAI_API_KEY");
    public static readonly LLMApiRegistryEntry Gemini = new("Gemini", "GEMINI_API_KEY");
    public static readonly LLMApiRegistryEntry Deepseek = new("Deepseek", "DEEPSEEK_API_KEY");
    public static readonly LLMApiRegistryEntry Groq = new("GroqCloud", "GROQ_API_KEY");
    public static readonly LLMApiRegistryEntry Anthropic = new("Anthropic", "ANTHROPIC_API_KEY");
    public static readonly LLMApiRegistryEntry Xai = new("Xai", "XAI_API_KEY");
    public static readonly LLMApiRegistryEntry Ollama = new("Ollama", "OLLAMA_API_KEY");
}

public record LLMApiRegistryEntry(string ApiName, string ApiKeyEnvName);