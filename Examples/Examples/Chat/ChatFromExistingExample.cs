using System.Text.Json;
using MaIN.Core.Hub;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatFromExistingExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        var result = AIHub.Chat()
            .WithModel<Qwen2_5_0_5b>();
        
        await result.WithMessage("What do you think about math theories?")
            .CompleteAsync();
        
        await result.WithMessage("And about physics?")
            .CompleteAsync();

        try
        {
            var chatNewContext = await AIHub.Chat().FromExisting(result.GetChatId());
            var messages = chatNewContext.GetChatHistory();
            Console.WriteLine(JsonSerializer.Serialize(messages));
        }
        catch (ChatNotFoundException ex)
        {
            Console.WriteLine(ex.PublicErrorMessage);
        }

    }
}