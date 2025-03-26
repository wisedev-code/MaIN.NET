using MaIN.Core.Hub;

namespace Examples;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");

        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        await context
            .WithMessage("Where do hedgehog goes at night?")
            .CompleteAsync(interactive: true);
    }
}