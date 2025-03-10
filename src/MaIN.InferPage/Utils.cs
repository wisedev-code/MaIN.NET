using MaIN.Domain.Entities;

namespace MaIN.InferPage;

public static class Utils
{
    public static string Model = "gemma2:2b";
    public static bool Visual => VisualModels.Contains(Model);
    private static string[] VisualModels = ["FLUX.1_Shnell", "dall-e-3"];
    public static bool OpenAi { get; set; }
    public static string Path { get; set; }
}

public static class Extensions
{
    public static bool IsInternal(this Message message)
    {
        return (bool)message.Properties?.Any(x => x is { Key: "agent_internal", Value: "true" });
    }
}