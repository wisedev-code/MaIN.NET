using Examples.Utils;
using MaIN.Core.Hub;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        
        ImagePreviewer.ShowImage(result.Message.Images);
    }
}