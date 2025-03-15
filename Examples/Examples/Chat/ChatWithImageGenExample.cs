using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatWithImageGenExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running!");
        
        var result = await AIHub.Chat()
            .EnableVisual()
            .WithMessage("Generate cyberpunk godzilla cat warrior")
            .CompleteAsync();
        
        ImagePreview.ShowImage(result.Message.Images);
    }
}