using System.Text.Json;
using MaIN.Core.Hub;

namespace Examples;

public class ChatFromExistingExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        var result = AIHub.Chat()
            .WithModel("qwen2.5:0.5b");
        
        await result.WithMessage("What do you think about math theories?")
            .CompleteAsync();
        
        await result.WithMessage("And about physics?")
            .CompleteAsync();

        var chatNewContext = await AIHub.Chat().FromExisting(result.GetChatId());
        var messages = chatNewContext.GetChatHistory();
        Console.WriteLine(JsonSerializer.Serialize(messages));
    }
}