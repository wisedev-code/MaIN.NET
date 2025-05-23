using System.Reflection;
using MaIN.Core.Hub;

namespace Examples;

public class ChatWithVisionExample : IExample
{
    /// <summary>
    /// Vision via Multimodal models as Llava is not supported yet
    /// </summary>
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> images = [Path.Combine(AppContext.BaseDirectory, "Files", "gamex.jpg")];
        
        var result = await AIHub.Chat()
            .WithModel("llama3.2:3b")
            .WithMessage("What is the title of game?")
            .WithFiles(images)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}