using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static string? Model = "gemma2:2b";
    public static bool Visual => VisualModels.Contains(Model);
    private static readonly string[] VisualModels = ["FLUX.1_Shnell", "dall-e-3"];
    public static bool OpenAi { get; set; }
    public static bool Gemini { get; set; }
    public static string? Path { get; set; }
    public static bool Reason { get; set; }
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
}
