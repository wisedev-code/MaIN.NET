using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;

namespace Examples.Mcp;

/// <summary>
/// Demonstrates MCP integration with Anthropic backend.
/// Uses @modelcontextprotocol/server-filesystem to write a fun fact to C:/Users/Public/funfacts/funfact.txt.
/// Anthropic uses native tool_use/tool_result protocol (not OpenAI-compatible).
/// </summary>
public class McpAnthropicExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpAnthropicExample is running!");
        Console.WriteLine("Uses native Anthropic tool_use protocol via MCP filesystem server.");
        Console.WriteLine("Output: C:/Users/Public/funfacts/funfact.txt");

        AnthropicExample.Setup();

        var result = await AIHub.Mcp()
            .WithBackend(BackendType.Anthropic)
            .WithConfig(new MaIN.Domain.Entities.Mcp
            {
                Name = "filesystem",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "C:/Users/Public"],
                Model = Models.Anthropic.ClaudeSonnet4
            })
            .PromptAsync(
                "Generate a fun fact (2-3 sentences, genuinely surprising) and write it to " +
                "C:/Users/Public/funfacts/funfact.txt using the write_file tool. " +
                "After writing, confirm what you saved and share the fun fact.");

        Console.WriteLine(result.Message.Content);
    }
}
