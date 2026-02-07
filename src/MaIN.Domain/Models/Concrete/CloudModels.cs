using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Models.Concrete;

// ===== OpenAI Models =====

public sealed record Gpt4oMini() : CloudModel(
    "gpt-4o-mini",
    BackendType.OpenAi,
    "GPT-4o Mini",
    ModelDefaults.DefaultMaxContextWindow,
    "Fast and affordable OpenAI model for everyday tasks");

public sealed record Gpt4_1Mini() : CloudModel(
    "gpt-4.1-mini",
    BackendType.OpenAi,
    "GPT-4.1 Mini",
    ModelDefaults.DefaultMaxContextWindow,
    "Updated mini model with improved capabilities");

public sealed record Gpt5Nano() : CloudModel(
    "gpt-5-nano",
    BackendType.OpenAi,
    "GPT-5 Nano",
    ModelDefaults.DefaultMaxContextWindow,
    "Next generation OpenAI nano model");

public sealed record DallE3() : CloudModel(
    "dall-e-3",
    BackendType.OpenAi,
    "DALL-E 3",
    4000,
    "Advanced image generation model from OpenAI");

// ===== Anthropic Models =====

public sealed record ClaudeSonnet4() : CloudModel(
    "claude-sonnet-4-20250514",
    BackendType.Anthropic,
    "Claude Sonnet 4",
    200000,
    "Latest Claude model with enhanced reasoning capabilities");

public sealed record ClaudeSonnet4_5() : CloudModel(
    "claude-sonnet-4-5-20250929",
    BackendType.Anthropic,
    "Claude Sonnet 4.5",
    200000,
    "Advanced Claude model with superior performance and extended context");

// ===== Gemini Models =====

public sealed record Gemini2_5Flash() : CloudModel(
    "gemini-2.5-flash",
    BackendType.Gemini,
    "Gemini 2.5 Flash",
    1000000,
    "Fast and efficient Google Gemini model for quick responses");

public sealed record Gemini2_0Flash() : CloudModel(
    "gemini-2.0-flash",
    BackendType.Gemini,
    "Gemini 2.0 Flash",
    1000000,
    "Google Gemini 2.0 flash model optimized for speed and efficiency");

// ===== xAI Models =====

public sealed record Grok3Beta() : CloudModel(
    "grok-3-beta",
    BackendType.Xai,
    "Grok 3 Beta",
    ModelDefaults.DefaultMaxContextWindow,
    "xAI latest Grok model in beta testing phase");

// ===== GroqCloud Models =====

public sealed record Llama3_1_8bInstant() : CloudModel(
    "llama-3.1-8b-instant",
    BackendType.GroqCloud,
    "Llama 3.1 8B Instant",
    8192,
    "Meta Llama 3.1 8B model optimized for fast inference on Groq hardware");

public sealed record GptOss20b() : CloudModel(
    "openai/gpt-oss-20b",
    BackendType.GroqCloud,
    "GPT OSS 20B",
    8192,
    "Open-source 20B parameter GPT model running on Groq infrastructure");

// ===== DeepSeek Models =====

public sealed record DeepSeekReasoner() : CloudModel(
    "deepseek-reasoner",
    BackendType.DeepSeek,
    "DeepSeek Reasoner",
    64000,
    "DeepSeek reasoning-focused model for complex problem solving"), IReasoningModel
{
    public Func<string, ThinkingState, LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt => null;
}

// ===== Ollama Models =====

public sealed record OllamaGemma3_4b() : CloudModel(
    "gemma3:4b",
    BackendType.Ollama,
    "Gemma3 4B (Ollama)",
    8192,
    "Balanced 4B model running on Ollama for writing, analysis, and mathematical reasoning");
