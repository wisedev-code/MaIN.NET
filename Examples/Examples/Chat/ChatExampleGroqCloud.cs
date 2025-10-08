using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples.Chat;

public class ChatExampleGroqCloud : IExample
{
    public async Task Start()
    {
        GroqCloudExample.Setup(); //We need to provide GroqCloud API key
        Console.WriteLine("(GroqCloud) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("llama-3.1-8b-instant")
            .WithMessage("Which color do people like the most?")
            .CompleteAsync(interactive: true);
    }
}