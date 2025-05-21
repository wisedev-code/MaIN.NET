using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;

namespace Examples;

public class McpExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpClientExample is running!");
        OpenAiExample.Setup();

        var result = await AIHub.Mcp()
            .WithBackend(BackendType.OpenAi)
            .WithConfig(
            new Mcp
            {
                Name = "AirBnB",
                Arguments = ["-y", "@openbnb/mcp-server-airbnb"],
                Command = "npx",
                Model = "gpt-4o-mini"
            })
            .PromptAsync("Show a listing of Madagascar properties");
        
        Console.WriteLine("⭐️ " + result.Message.Content + " ⭐️");
    }
}