using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static string? Model = "gemma2:2b";
    public static bool Visual => VisualModels.Contains(Model);
    private static readonly string[] VisualModels = ["FLUX.1_Shnell", "FLUX.1", "dall-e-3", "dall-e", "imagen", "imagen-3"]; //user might type different names
    public static bool OpenAi { get; set; }
    public static bool Gemini { get; set; }
    public static bool DeepSeek { get; set; }
    public static bool GroqCloud { get; set; }
    public static bool Anthropic { get; set; }
    public static bool Xai { get; set; }
    public static bool Ollama { get; set; }
    public static string? Path { get; set; }
    public static bool Reason { get; set; }
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
}
