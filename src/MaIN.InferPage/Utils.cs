using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Abstract;

namespace MaIN.InferPage;

public static class Utils
{
    public static BackendType BackendType { get; set; } = BackendType.Self;
    public static bool HasApiKey { get; set; }
    public static string? Path { get; set; }
    public static bool IsLocal => BackendType == BackendType.Self || (BackendType == BackendType.Ollama && !HasApiKey);
    public static string? Model = "gemma3-4b";
    public static bool Reason
    {
        get
        {
            if (string.IsNullOrEmpty(Model)) return false;
            if (ModelRegistry.TryGetById(Model, out var m))
                return m is IReasoningModel && !ImageGen; // reasoning and image gen are mutually exclusive
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
            return VisionModels.Contains(Model); // fallback for unregistered models
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
