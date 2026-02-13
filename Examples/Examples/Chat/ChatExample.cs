using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");

        // Using strongly-typed model
        await AIHub.Chat()
            .WithModel<Gemma2_2b>()
            .EnsureModelDownloaded()
            .WithMessage("Where do hedgehogs goes at night?")
            .CompleteAsync(interactive: true);
    }
}