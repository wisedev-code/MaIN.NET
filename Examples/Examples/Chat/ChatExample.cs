using MaIN.Core.Hub;

namespace Examples;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");

        var context = await AIHub.Chat().WithModel("gemma2:2b")
            .WithMessage("Generate a 4 sentence poem.")
            .CompleteAsync();

        Console.WriteLine(context.Message.Content);
        
        await AIHub.Chat()
            .WithTTS("kokoro", @"D:\Models\kokoro.onnx", "af_heart")
            .WithMessage(context.Message.Content).CompleteAsync();
    }
}