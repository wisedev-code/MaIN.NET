using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatExampleGroqCloud : IExample
{
    public async Task Start()
    {
        GroqCloudExample.Setup(); //We need to provide GroqCloud API key
        Console.WriteLine("(GroqCloud) ChatExample is running!");

        await AIHub.Chat()
            .WithModel<Llama3_1_8bInstant>()
            .WithMessage("Which color do people like the most?")
            .CompleteAsync(interactive: true);
    }
}