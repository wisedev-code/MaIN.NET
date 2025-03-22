using MaIN.Core.Hub;

namespace Examples;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");
        
        var context = AIHub.Chat().WithCustomModel("test","/Users/pstach/WiseDev/Models/DeepSeekR1-8b.gguf");
        
        await context
            .WithMessage("Where the hedgehog goes at night?")
            .CompleteAsync(interactive: true);
    }
}