using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Chat;

public class ChatExampleGemini : IExample
{
    public async Task Start()
    {
        GeminiExample.Setup(); //We need to provide Gemini API key
        Console.WriteLine("(Gemini) ChatExample is running!");

        await AIHub.Chat()
            .WithModel(Models.Gemini.Gemini2_5Flash)
            .WithMessage("Is the killer whale the smartest animal?")
            .CompleteAsync(interactive: true);
    }
}
