using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatExampleGroqCloud : IExample
{
    public async Task Start()
    {
        GroqCloudExample.Setup(); //We need to provide GroqCloud API key
        Console.WriteLine("(GroqCloud) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("llama3-8b-8192")
            .WithMessage("Which color do people like the most?")
            .CompleteAsync(interactive: true);
    }
}