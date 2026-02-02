using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatExampleOpenAi : IExample
{
    public async Task Start()
    {
        OpenAiExample.Setup(); //We need to provide OpenAi API key
        
        Console.WriteLine("(OpenAi) ChatExample is running!"); 
        
        await AIHub.Chat()
            .WithModel<Gpt5Nano>()
            .WithMessage("What do you consider to be the greatest invention in history?")
            .CompleteAsync(interactive: true);
    }
}