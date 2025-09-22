using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatExampleGemini : IExample
{
    public async Task Start()
    {
        GeminiExample.Setup(); //We need to provide Gemini API key
        Console.WriteLine("(Gemini) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("gemini-2.5-flash")
            .WithMessage("Is the killer whale the smartest animal?")
            .CompleteAsync(interactive: true);
    }
}