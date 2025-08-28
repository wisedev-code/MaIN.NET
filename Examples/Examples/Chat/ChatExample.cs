using MaIN.Core.Hub;

namespace Examples;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");

        await AIHub.Chat()
            .WithModel("gemma2:2b")
            .WithMessage("Where do hedgehogs goes at night?")
            .CompleteAsync(interactive: true);
    }
}