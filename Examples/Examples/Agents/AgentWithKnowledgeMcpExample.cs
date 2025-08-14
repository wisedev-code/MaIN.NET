using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using Microsoft.Identity.Client;

namespace Examples.Agents;

public class AgentWithKnowledgeMcpExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("");

        var context = await AIHub.Agent()
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddMcp(new Mcp
                {
                    Name = "DuckDuckGo",
                    Command = "npx",
                    Arguments = ["-y", "duckduckgo-mcp-server"],
                    Backend = BackendType.Gemini,
                    Model = "gemini-2.0-flash"
                }, ["search", "browser", "duck_duck_go", "research"])
                .AddMcp(new Mcp
                {
                    Name = "FileSystem",
                    Command = "npx",
                    Arguments = ["-y",
                        "@modelcontextprotocol/server-filesystem",
                        "/Users/pstach/Desktop",
                        "/Users/pstach/WiseDev"],
                    Backend = BackendType.GroqCloud,
                    Model = "openai/gpt-oss-20b"
                }, ["filesystem", "file operations", "read write", "disk search"])
                .AddMcp(new Mcp
                {
                    Name = "FileSystem",
                    Command = "npx",
                    Arguments = ["-y",
                        "@modelcontextprotocol/server-filesystem",
                        "/Users/pstach/Desktop",
                        "/Users/pstach/WiseDev"],
                    Backend = BackendType.OpenAi,
                    Model = "gpt-5-nano"
                }, ["filesystem", "file operations", "read write", "disk search"])
                .Build())
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();

        var result = await context
            .ProcessAsync("");

        Console.WriteLine(result.Message.Content);
    }
}