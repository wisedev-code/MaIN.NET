using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static string? Model = "gpt-4o-mini";
    public static bool Visual => VisualModels.Contains(Model);
    private static readonly string[] VisualModels = ["FLUX.1_Shnell", "dall-e-3"];
    public static bool OpenAi { get; set; } = true;
    public static string? Path { get; set; }
    public static bool Reason { get; set; }
}

public class MessageExt
{
    public required Message Message { get; set; }
    public bool ShowReason { get; set; }
}
