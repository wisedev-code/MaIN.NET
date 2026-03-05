using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Abstract;
using MaIN.Domain.Models.Concrete;

namespace MaIN.InferPage;

public static class Utils
{
    public static BackendType BackendType { get; set; } = BackendType.Self;
    public static bool HasApiKey { get; set; }
    public static string? Path { get; set; }
    public static bool IsLocal => BackendType == BackendType.Self || (BackendType == BackendType.Ollama && !HasApiKey);
    public static string? Model = "gemma3-4b";
    public static bool NeedsConfiguration { get; set; }

    // Manual capability overrides for unregistered models (set from Settings UI)
    public static bool? ManualVision { get; set; }
    public static bool? ManualReasoning { get; set; }
    public static bool? ManualImageGen { get; set; }

    public static bool Reason
    {
        get
        {
            if (string.IsNullOrEmpty(Model)) return false;
            if (ModelRegistry.TryGetById(Model, out var m))
                return m is IReasoningModel && !ImageGen; // reasoning and image gen are mutually exclusive
            if (ManualReasoning.HasValue) return ManualReasoning.Value;
            return false;
        }
    }

    public static bool ImageGen
    {
        get
        {
            if (string.IsNullOrEmpty(Model)) return false;
            if (ModelRegistry.TryGetById(Model, out var m))
                return m is IImageGenerationModel;
            if (ManualImageGen.HasValue) return ManualImageGen.Value;
            return ImageGenerationModels.Contains(Model); // fallback for unregistered models (e.g. FLUX via separate server)
        }
    }

    public static bool Vision
    {
        get
        {
            if (string.IsNullOrEmpty(Model)) return false;
            if (ModelRegistry.TryGetById(Model, out var m))
                return m is IVisionModel;
            if (ManualVision.HasValue) return ManualVision.Value;
            return VisionModels.Contains(Model); // fallback for unregistered models
        }
    }

    public static bool IsKnownVisionModel(string model) => VisionModels.Contains(model);
    public static bool IsKnownImageGenModel(string model) => ImageGenerationModels.Contains(model);

    public static void ApplySettings(
        BackendType backendType,
        string model,
        string? modelPath,
        bool hasVision,
        bool hasReasoning,
        bool hasImageGen,
        MaINSettings mainSettings,
        string? apiKey)
    {
        BackendType = backendType;
        Model = model;
        // Strip leading/trailing whitespace and invisible Unicode control/format characters
        // (e.g. U+202A LEFT-TO-RIGHT EMBEDDING, U+FEFF BOM — added by Windows Explorer on copy-paste)
        if (!string.IsNullOrWhiteSpace(modelPath))
        {
            var cleaned = modelPath.Trim();
            int i = 0;
            while (i < cleaned.Length &&
                   (char.IsControl(cleaned[i]) ||
                    char.GetUnicodeCategory(cleaned[i]) == System.Globalization.UnicodeCategory.Format))
                i++;
            Path = i < cleaned.Length ? cleaned.Substring(i) : null;
        }
        else
        {
            Path = null;
        }
        HasApiKey = !string.IsNullOrEmpty(apiKey);
        NeedsConfiguration = false;

        ManualVision = hasVision;
        ManualReasoning = hasReasoning;
        ManualImageGen = hasImageGen;

        mainSettings.BackendType = backendType;

        if (!string.IsNullOrEmpty(apiKey))
        {
            var entry = LLMApiRegistry.GetEntry(backendType);
            if (entry != null)
            {
                Environment.SetEnvironmentVariable(entry.ApiKeyEnvName, apiKey);
            }

            switch (backendType)
            {
                case BackendType.OpenAi: mainSettings.OpenAiKey = apiKey; break;
                case BackendType.Gemini: mainSettings.GeminiKey = apiKey; break;
                case BackendType.DeepSeek: mainSettings.DeepSeekKey = apiKey; break;
                case BackendType.Anthropic: mainSettings.AnthropicKey = apiKey; break;
                case BackendType.GroqCloud: mainSettings.GroqCloudKey = apiKey; break;
                case BackendType.Ollama: mainSettings.OllamaKey = apiKey; break;
                case BackendType.Xai: mainSettings.XaiKey = apiKey; break;
            }
        }
        else
        {
            // Clear stale key for this backend (e.g. switching Ollama Cloud → Local)
            var entry = LLMApiRegistry.GetEntry(backendType);
            if (entry != null)
            {
                Environment.SetEnvironmentVariable(entry.ApiKeyEnvName, null);
            }

            switch (backendType)
            {
                case BackendType.OpenAi: mainSettings.OpenAiKey = null; break;
                case BackendType.Gemini: mainSettings.GeminiKey = null; break;
                case BackendType.DeepSeek: mainSettings.DeepSeekKey = null; break;
                case BackendType.Anthropic: mainSettings.AnthropicKey = null; break;
                case BackendType.GroqCloud: mainSettings.GroqCloudKey = null; break;
                case BackendType.Ollama: mainSettings.OllamaKey = null; break;
                case BackendType.Xai: mainSettings.XaiKey = null; break;
            }
        }
    }

    private static readonly HashSet<string> ImageGenerationModels =
    [
        "FLUX.1_Shnell", "FLUX.1",
        "dall-e-3", "dall-e",
        "gpt-image-1",
        "imagen", "imagen-3", "imagen-4", "imagen-4-fast",
        "grok-2-image"
    ];

    private static readonly HashSet<string> VisionModels =
    [
        "gpt-4o", "gpt-4o-mini",
        "claude-3-opus", "claude-3-sonnet", "claude-3-haiku",
        "claude-3-5-sonnet", "claude-3-5-haiku", "claude-3-7-sonnet",
        "gemini-1.5-pro", "gemini-1.5-flash", "gemini-2.0-flash", "gemini-2.0-flash-lite",
        "llava", "llava-1.6", "llava-phi3",
        "gemma3", "gemma3-4b", "gemma3-12b", "gemma3-27b"
    ];
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
    public bool HasReasoning { get; set; }
    public string ComputedContent { get; set; } = string.Empty;
    public string ComputedReasoning { get; set; } = string.Empty;
    public List<string> AttachedFiles { get; set; } = new();
    public List<(string Name, string Base64)> AttachedImages { get; set; } = new();
}