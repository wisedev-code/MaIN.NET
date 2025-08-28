using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;

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
            new MaIN.Domain.Entities.Mcp
            {
                Name = "McpEverythingDemo",
                Arguments = ["-y", "@modelcontextprotocol/server-everything"],
                Command = "npx",
                Model = "gpt-4o-mini"
            })
            .PromptAsync("Provide me information about resource 21 and 37. Also explain how you get this data");
        
        Console.WriteLine(result.Message.Content);
    }
}