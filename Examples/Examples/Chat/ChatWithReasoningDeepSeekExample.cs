using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithReasoningDeepSeekExample : IExample
{
    public async Task Start()
    {
        DeepSeekExample.Setup(); //We need to provide DeepSeek API key
        Console.WriteLine("(DeepSeek) ChatExample with reasoning is running!");

        await AIHub.Chat()
            .WithModel<DeepSeekReasoner>() // a model that supports reasoning
            .WithMessage("What chill pc game do you recommend?")
            .CompleteAsync(interactive: true);
    }
}