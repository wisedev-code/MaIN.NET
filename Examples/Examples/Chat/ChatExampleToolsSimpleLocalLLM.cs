using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Chat;

public class ChatExampleToolsSimpleLocalLLM : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Local LLM ChatExample with tools is running!");

        var model = AIHub.Model();

        await AIHub.Chat()
            .WithModel("gemma3:4b")
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