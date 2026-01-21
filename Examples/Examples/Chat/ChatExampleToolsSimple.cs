using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Chat;

public class ChatExampleToolsSimple : IExample
{
    public async Task Start()
    {
        OpenAiExample.Setup(); //We need to provide OpenAi API key
        
        Console.WriteLine("(OpenAi) ChatExample with tools is running!");

        var model = AIHub.Model();
        
        await AIHub.Chat()
            .WithModel("gpt-5-nano")
            .WithMessage("What time is it right now?")
            .WithTools(new ToolsConfigurationBuilder()
                .AddTool(
                    name: "get_current_time",
                    description: "Get the current date and time",
                    execute: Tools.GetCurrentTime) 
                .WithToolChoice("auto")
                .Build())
            .CompleteAsync(interactive: true);
    }
}
