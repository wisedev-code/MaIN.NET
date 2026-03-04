using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Chat;

public class ChatExampleXai : IExample
{
    public async Task Start()
    {
        XaiExample.Setup(); //We need to provide xAI API key
        Console.WriteLine("(xAI) ChatExample is running!");

        await AIHub.Chat()
            .WithModel(Models.Xai.Grok3Beta)
            .WithMessage("Is the killer whale cute?")
            .CompleteAsync(interactive: true);
    }
}
