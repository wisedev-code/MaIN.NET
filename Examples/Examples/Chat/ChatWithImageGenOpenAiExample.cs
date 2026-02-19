using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithImageGenOpenAiExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running! (OpenAi)");
        OpenAiExample.Setup(); // We need to provide OpenAi API key
        
        var result = await AIHub.Chat()
            .WithModel<DallE3>()
            .EnableVisual()
            .WithMessage("Generate rock style cow playing guitar")
            .CompleteAsync();
        
        ImagePreview.ShowImage(result.Message.Image);
    }
}