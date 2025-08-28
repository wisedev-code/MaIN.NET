using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatExampleAnthropic : IExample
{
    public async Task Start()
    {
        AnthropicExample.Setup(); //We need to provide Anthropic API key
        Console.WriteLine("(Anthropic) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("claude-sonnet-4-20250514")
            .WithMessage("Write a haiku about programming on Monday morning.")
            .CompleteAsync(interactive: true);
    }
}