using MaIN.Core.Hub;

namespace Examples;

public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!"); //TODO
        
        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        var result = await context
            .WithMessage("Where the hedgehog goes at night?")
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}