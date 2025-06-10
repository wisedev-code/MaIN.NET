using System.Reflection;
using MaIN.Core.Hub;
using OllamaSharp.Models.Chat;

namespace Examples;

public class ChatWithVisionExample : IExample
{
    /// <summary>
    /// Vision via Multimodal models as Llava is not supported yet
    /// </summary>
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        var image = await File.ReadAllBytesAsync(
            Path.Combine(AppContext.BaseDirectory, "Files", "gamex.jpg"));
        
        var result = await AIHub.Chat()
            .WithCustomModel("QwenVL",
                path: "/Users/pstach/WiseDev/Models/llava-v1.6-mistral-7b.Q4_K_M.gguf",
                mmProject: "/Users/pstach/WiseDev/Models/mmproj-model-f16.gguf")
            .WithMessage("What can you see on the image?", image)
            .CompleteAsync(interactive: true);
        
        Console.WriteLine(result.Message.Content);
    }
}