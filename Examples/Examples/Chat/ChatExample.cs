using MaIN.Core.Hub;

namespace Examples;

public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");
        
        var context = AIHub.Chat().WithCustomModel("model", 
            "/Users/pstach/WiseDev/Models/Qwen2.5-coder-14b.gguf");
        
        await context
            .WithMessage("Where the hedgehog goes at night?")
            .CompleteAsync(interactive: true);
    }
}