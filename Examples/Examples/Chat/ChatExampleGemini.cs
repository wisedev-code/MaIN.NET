using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatExampleGemini : IExample
{
    public async Task Start()
    {
        GeminiExample.Setup(); //We need to provide Gemini API key
        Console.WriteLine("(Gemini) ChatExample is running!");

        // Get built-in Gemini 2.5 Flash model
        var model = AIHub.Model().GetModel(new Gemini2_5Flash().Id);

        // Or create the model manually if not available in the hub
        var customModel = new GenericCloudModel(
            "gemini-2.5-flash",
            BackendType.Gemini
        );

        await AIHub.Chat()
            .WithModel(customModel)
            .WithMessage("Is the killer whale the smartest animal?")
            .CompleteAsync(interactive: true);
    }
}
