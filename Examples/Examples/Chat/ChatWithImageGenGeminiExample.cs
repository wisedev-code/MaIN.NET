using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatWithImageGenGeminiExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running! (Gemini)");
        GeminiExample.Setup(); // We need to provide Gemini API key

        var result = await AIHub.Chat()
            .EnableVisual()
            .WithMessage("Generate hamster as a astronaut on the moon")
            .CompleteAsync();

        ImagePreview.ShowImage(result.Message.Images);
    }
}