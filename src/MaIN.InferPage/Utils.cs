using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static BackendType BackendType { get; set; } = BackendType.Self;
    public static bool HasApiKey { get; set; }
    public static bool IsLocal => BackendType == BackendType.Self || (BackendType == BackendType.Ollama && !HasApiKey);
    public static string? Model = "gemma3:4b";
    public static bool Reason { get; set; }
    public static bool Visual => VisualModels.Contains(Model);
    private static readonly string[] VisualModels = ["FLUX.1_Shnell", "FLUX.1", "dall-e-3", "dall-e", "imagen", "imagen-3"]; //user might type different names
    public static string? Path { get; set; }
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
    public List<string> AttachedFiles { get; set; } = new();
}
