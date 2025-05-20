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
                Name = "Git-Ingest",
                Arguments = ["--from", "git+https://github.com/adhikasp/mcp-git-ingest", "mcp-git-ingest"],
                Command = "uvx",
                Model = "gpt-4o-mini"
            })
            .PromptAsync("How many stars does MaIN.NET repository have?");
        
        Console.WriteLine("⭐️ " + result.Message.Content + " ⭐️");
    }
}