using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static string? Model = "deepseekr1-8b";
    public static bool Visual => VisualModels.Contains(Model);
    private static readonly string[] VisualModels = ["FLUX.1_Shnell", "dall-e-3"];
    public static bool OpenAi { get; set; }
    public static string? Path { get; set; }
}
