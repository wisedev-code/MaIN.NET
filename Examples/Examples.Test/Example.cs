using MaIN.Core.Hub;

namespace Examples.Test;

public class Example : IExample
{
    public async Task Start()
    {
        Console.WriteLine("TestExample is running!");
        
        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        var result = await context
            .WithMessage("Where the hedgehog goes at night?")
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}