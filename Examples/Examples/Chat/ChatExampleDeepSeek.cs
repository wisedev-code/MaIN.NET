using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatExampleDeepSeek : IExample
{
    public async Task Start()
    {
        DeepSeekExample.Setup(); //We need to provide Gemini API key
        Console.WriteLine("(DeepSeek) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("deepseek-chat")
            .WithMessage("What chill pc game do you recommend?")
            .CompleteAsync(interactive: true);
    }
}