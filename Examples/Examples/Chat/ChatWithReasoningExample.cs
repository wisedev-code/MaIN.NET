using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithReasoningExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithReasoningExample is running!");

        await AIHub.Chat()
            .WithModel<DeepSeek_R1_1_5b>()
            .WithMessage("Think about greatest poet of all time")
            .CompleteAsync(interactive: true);
    }
}