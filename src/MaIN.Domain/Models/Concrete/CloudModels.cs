using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Models.Concrete;

// ===== OpenAI Models =====

public sealed record Gpt4oMini() : CloudModel(
    Models.OpenAi.Gpt4oMini,
    BackendType.OpenAi,
    "GPT-4o Mini",
    ModelDefaults.DefaultMaxContextWindow,
    "Fast and affordable OpenAI model for everyday tasks"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Gpt4_1Mini() : CloudModel(
    Models.OpenAi.Gpt4_1Mini,
    BackendType.OpenAi,
    "GPT-4.1 Mini",
    ModelDefaults.DefaultMaxContextWindow,
    "Updated mini model with improved capabilities"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Gpt5Nano() : CloudModel(
    Models.OpenAi.Gpt5Nano,
    BackendType.OpenAi,
    "GPT-5 Nano",
    ModelDefaults.DefaultMaxContextWindow,
    "Next generation OpenAI nano model"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record DallE3() : CloudModel(
    Models.OpenAi.DallE3,
    BackendType.OpenAi,
    "DALL-E 3",
    4000,
    "Advanced image generation model from OpenAI"), IImageGenerationModel;

public sealed record GptImage1() : CloudModel(
    Models.OpenAi.GptImage1,
    BackendType.OpenAi,
    "GPT Image 1",
    4000,
    "OpenAI's latest image generation model"), IImageGenerationModel;

// ===== Anthropic Models =====

public sealed record ClaudeSonnet4() : CloudModel(
    Models.Anthropic.ClaudeSonnet4,
    BackendType.Anthropic,
    "Claude Sonnet 4",
    200000,
    "Latest Claude model with enhanced reasoning capabilities"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record ClaudeSonnet4_5() : CloudModel(
    Models.Anthropic.ClaudeSonnet4_5,
    BackendType.Anthropic,
    "Claude Sonnet 4.5",
    200000,
    "Advanced Claude model with superior performance and extended context"), IVisionModel
{
    public string? MMProjectName => null;
}

// ===== Gemini Models =====

public sealed record Gemini2_5Flash() : CloudModel(
    Models.Gemini.Gemini2_5Flash,
    BackendType.Gemini,
    "Gemini 2.5 Flash",
    1000000,
    "Fast and efficient Google Gemini model for quick responses"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Gemini2_0Flash() : CloudModel(
    Models.Gemini.Gemini2_0Flash,
    BackendType.Gemini,
    "Gemini 2.0 Flash",
    1000000,
    "Google Gemini 2.0 flash model optimized for speed and efficiency"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Gemini2_5Pro() : CloudModel(
    Models.Gemini.Gemini2_5Pro,
    BackendType.Gemini,
    "Gemini 2.5 Pro",
    1000000,
    "Google's most capable Gemini model"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record GeminiImagen4_0FastGenerate() : CloudModel(
    Models.Gemini.Imagen4_0_FastGenerate,
    BackendType.Gemini,
    "Imagen 4.0 Fast (Gemini)",
    4000,
    "Google's fast image generation model via Gemini API"), IImageGenerationModel;

public sealed record GeminiNanoBanana() : CloudModel(
    Models.Gemini.NanoBanana,
    BackendType.Gemini,
    "Gemini 2.5 Flash Image (NanoBanana)",
    130000,
    "Google’s high-speed, high-fidelity image generation via Gemini API."), IImageGenerationModel;

// ===== Vertex AI Models =====

public sealed record VertexGemini2_5Pro() : CloudModel(
    Models.Vertex.Gemini2_5Pro,
    BackendType.Vertex,
    "Gemini 2.5 Pro (Vertex)",
    1000000,
    "Fast and efficient Gemini model served via Vertex AI"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record VertexGemini2_5Flash() : CloudModel(
    Models.Vertex.Gemini2_5Flash,
    BackendType.Vertex,
    "Gemini 2.5 Flash (Vertex)",
    1000000,
    "Fast and efficient Gemini model served via Vertex AI"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record VertexVeo2_0Generate() : CloudModel(
    Models.Vertex.Veo2_0_Generate,
    BackendType.Vertex,
    "Veo 2.0 Generate",
    4000,
    "Google's video generation model available through Vertex AI"), IImageGenerationModel;

public sealed record VertexImagen4_0Generate() : CloudModel(
    Models.Vertex.Imagen4_0_Generate,
    BackendType.Vertex,
    "Imagen 4.0 (Vertex)",
    4000,
    "Google's latest image generation model available through Vertex AI"), IImageGenerationModel;

// ===== xAI Models =====

public sealed record Grok4_20Reasoning() : CloudModel(
    Models.Xai.Grok4_20Reasoning,
    BackendType.Xai,
    "Grok 4.20 reasoning",
    2_000_000,
    "A xai flagship model, offering fast, agentic tool use with low hallucination and strong prompt adherence for precise, reliable responses."), IVisionModel, IReasoningModel
{
    public string? MMProjectName => null;
    public Func<string, ThinkingState, LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt => null;
}

public sealed record Grok4_20NonReasoning() : CloudModel(
    Models.Xai.Grok4_20NonReasoning,
    BackendType.Xai,
    "Grok 4.20 non reasoning",
    2_000_000,
    "A xai flagship model, offering fast, agentic tool use with low hallucination and strong prompt adherence for precise, reliable responses."), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Grok4_1FastReasoning() : CloudModel(
    Models.Xai.Grok4_1FastReasoning,
    BackendType.Xai,
    "Grok 4.1 fast reasoning",
    2_000_000,
    "A xai multimodal model optimized specifically for high-performance agentic tool calling"), IVisionModel, IReasoningModel
{
    public string? MMProjectName => null;
    public Func<string, ThinkingState, LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt => null;
}

public sealed record Grok4_1Fast() : CloudModel(
    Models.Xai.Grok4_1FastNonReasoning,
    BackendType.Xai,
    "Grok 4.1 fast",
    2_000_000,
    "A xai multimodal model optimized specifically for high-performance agentic tool calling"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record Grok3Beta() : CloudModel(
    Models.Xai.Grok3Beta,
    BackendType.Xai,
    "Grok 3 Beta",
    ModelDefaults.DefaultMaxContextWindow,
    "xAI latest Grok model in beta testing phase"), IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record GrokImage() : CloudModel(
    Models.Xai.GrokImage,
    BackendType.Xai,
    "Grok 2 Image",
    4000,
    "xAI image generation model"), IImageGenerationModel;

public sealed record GrokImagineImage() : CloudModel(
    Models.Xai.GrokImagineImage,
    BackendType.Xai,
    "Grok Imagine Image",
    4000,
    "xAI image generation model"), IImageGenerationModel, IVisionModel
{
    public string? MMProjectName => null;
}

public sealed record GrokImagineImagePro() : CloudModel(
    Models.Xai.GrokImagineImagePro,
    BackendType.Xai,
    "Grok Imagine Image Pro",
    4000,
    "xAI image generation model"), IImageGenerationModel, IVisionModel
{
    public string? MMProjectName => null;
}

// ===== GroqCloud Models =====

public sealed record Llama3_1_8bInstant() : CloudModel(
    Models.Groq.Llama3_1_8b,
    BackendType.GroqCloud,
    "Llama 3.1 8B Instant",
    8192,
    "Meta Llama 3.1 8B model optimized for fast inference on Groq hardware");

public sealed record Llama3_3_70bVersatile() : CloudModel(
    Models.Groq.Llama3_3_70b,
    BackendType.GroqCloud,
    "Llama 3.3 70B Versatile",
    130_000,
    "Meta's efficient, high-performance multilingual language model");

public sealed record GptOss20b() : CloudModel(
    Models.Groq.GptOss20b,
    BackendType.GroqCloud,
    "GPT OSS 20B",
    8192,
    "Open-source 20B parameter GPT model running on Groq infrastructure");

public sealed record GptOss120b() : CloudModel(
    Models.Groq.GptOss120b,
    BackendType.GroqCloud,
    "GPT OSS 120B",
    130_000,
    "Open-source 120B parameter GPT model running on Groq infrastructure");

// ===== DeepSeek Models =====

public sealed record DeepSeekReasoner() : CloudModel(
    Models.DeepSeek.Reasoner,
    BackendType.DeepSeek,
    "DeepSeek Reasoner",
    128_000,
    "DeepSeek reasoning-focused model for complex problem solving"), IReasoningModel
{
    public Func<string, ThinkingState, LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt => null;
}

public sealed record DeepSeekChat() : CloudModel(
    Models.DeepSeek.Chat,
    BackendType.DeepSeek,
    "DeepSeek Chat",
    128_000,
    "DeepSeek model for complex problem solving");

// ===== Ollama Models =====

public sealed record OllamaGemma3_4b() : CloudModel(
    Models.Ollama.Gemma3_4b,
    BackendType.Ollama,
    "Gemma3 4B (Ollama)",
    8192,
    "Balanced 4B model running on Ollama for writing, analysis, and mathematical reasoning"), IVisionModel
{
    public string? MMProjectName => null;
}
