using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples;

public class ChatExampleClaude : IExample
{
    public async Task Start()
    {
        ClaudeExample.Setup(); //We need to provide Claude API key
        Console.WriteLine("(Claude) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("claude-sonnet-4-20250514")
            .WithMessage("Why the clouds are white?")
            .CompleteAsync(interactive: true);
    }
}