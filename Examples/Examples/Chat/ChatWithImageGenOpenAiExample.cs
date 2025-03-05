using Examples.Utils;
using MaIN.Core.Hub;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Examples;

public class ChatWithImageGenOpenAiExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running! (OpenAi)");
        OpenAiExample.Setup(); // We need to provide OpenAi API key
        
        var result = await AIHub.Chat()
            .EnableVisual()
            .WithMessage("Generate rock style cow playing guitar")
            .CompleteAsync();
        
        ImagePreview.ShowImage(result.Message.Images);
    }
}