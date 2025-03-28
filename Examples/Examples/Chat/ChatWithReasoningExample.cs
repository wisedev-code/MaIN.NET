using MaIN.Core.Hub;

namespace Examples;

public class ChatWithReasoningExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithReasoningExample is running!");

        await AIHub.Chat()
            .WithModel("deepseekR1:1.5b")
            .WithMessage("Think about greatest poet of all time")
            .CompleteAsync(interactive: true);
    }
}