namespace MaIN.Domain.Models;

/// <summary>
/// Compile-time constants for all pre-built model IDs.
/// Use these with <c>WithModel(string modelId)</c> to avoid magic strings.
/// </summary>
public static class Models
{
    public static class OpenAi
    {
        public const string Gpt4oMini = "gpt-4o-mini";
        public const string Gpt4_1Mini = "gpt-4.1-mini";
        public const string Gpt5Nano = "gpt-5-nano";
        public const string DallE3 = "dall-e-3";
        public const string GptImage1 = "gpt-image-1";
    }

    public static class Anthropic
    {
        public const string ClaudeSonnet4 = "claude-sonnet-4-20250514";
        public const string ClaudeSonnet4_5 = "claude-sonnet-4-5-20250929";
    }

    public static class Gemini
    {
        public const string Gemini2_5Flash = "gemini-2.5-flash";
        public const string Gemini2_0Flash = "gemini-2.0-flash";
    }

    public static class Xai
    {
        public const string Grok3Beta = "grok-3-beta";
        public const string GrokImage = "grok-2-image";
    }

    public static class Groq
    {
        public const string Llama3_1_8bInstant = "llama-3.1-8b-instant";
        public const string GptOss20b = "openai/gpt-oss-20b";
    }

    public static class DeepSeek
    {
        public const string Reasoner = "deepseek-reasoner";
    }

    public static class Ollama
    {
        public const string Gemma3_4b = "gemma3:4b";
    }

    public static class Vertex
    {
        public const string Gemini2_5Pro = "google/gemini-2.5-pro";
        public const string Gemini2_5Flash = "google/gemini-2.5-flash";
        public const string Veo2_0_Generate = "google/veo-2.0-generate-001";
    }

    public static class Local
    {
        // Gemma
        public const string Gemma2_2b = "gemma2-2b";
        public const string Gemma3_4b = "gemma3-4b";
        public const string Gemma3_12b = "gemma3-12b";
        public const string Gemma3n_e4b = "gemma3n-e4b";

        // Llama
        public const string Llama3_2_3b = "llama3.2-3b";
        public const string Llama3_1_8b = "llama3.1-8b";
        public const string Llava_7b = "llava-7b";
        public const string Llava16Mistral_7b = "llava-1.6-mistral-7b";

        // Hermes
        public const string Hermes3_3b = "hermes3-3b";
        public const string Hermes3_8b = "hermes3-8b";

        // Qwen
        public const string Qwen2_5_0_5b = "qwen2.5-0.5b";
        public const string Qwen2_5_Coder_3b = "qwen2.5-coder-3b";
        public const string Qwen2_5_Coder_7b = "qwen2.5-coder-7b";
        public const string Qwen2_5_Coder_14b = "qwen2.5-coder-14b";
        public const string Qwen3_8b = "qwen3-8b";
        public const string Qwen3_14b = "qwen3-14b";
        public const string QwQ_7b = "qwq-7b";

        // DeepSeek
        public const string DeepSeekR1_8b = "deepseekr1-8b";
        public const string DeepSeekR1_1_5b = "deepseekr1-1.5b";

        // Phi
        public const string Phi3_5_3b = "phi3.5-3b";
        public const string Phi4_4b = "phi4-4b";

        // Other
        public const string Lfm2_1_2b = "lfm2-1.2b";
        public const string Minicpm4_8b = "minicpm4-8b";
        public const string Mistral3_2_24b = "mistral-3.2-24b";
        public const string Webgen_4b = "webgen-4b";
        public const string Bielik2_5_11b = "bielik-2.5-11b";
        public const string OlympicCoder_7b = "olympiccoder-7b";
        public const string Yi_6b = "yi-6b";
        public const string Smollm2_0_1b = "smollm2-0.1b";
        public const string Olmo2_7b = "olmo2-7b";

        // Embedding
        public const string NomicEmbedding = "nomic-embedding";

        // TTS
        public const string Kokoro82m = "kokoro-82m";

        // Image Generation
        public const string Flux1Shnell = "FLUX.1_Shnell";
    }
}
