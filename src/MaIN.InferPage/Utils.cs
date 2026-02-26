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
    public static bool Reason { get; set; }
    public static bool Visual
    {
        get
        {
            if (string.IsNullOrEmpty(Model)) return false;
            if (ModelRegistry.TryGetById(Model, out var m))
                return m is IImageGenerationModel;
            return ImageGenerationModels.Contains(Model); // fallback for unregistered models (e.g. FLUX via separate server)
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
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
    public List<string> AttachedFiles { get; set; } = new();
    public List<(string Name, string Base64)> AttachedImages { get; set; } = new();
}
