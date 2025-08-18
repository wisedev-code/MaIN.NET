using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;

namespace Examples.Agents;

public class AgentWithKnowledgeMcpExample : IExample
{
    public async Task Start()
    {
        //Note: to run this example, you should do 'gh auth login' to give octocode mcp server access to github CLI
        Console.WriteLine("Agent with knowledge base example MCP sources");

        var context = await AIHub.Agent()
            .WithBackend(BackendType.OpenAi)
            .WithModel("gpt-4.1-mini")
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
                    Name = "Octocode",
                    Command = "npx",
                    Arguments = ["octocode-mcp"],
                    Backend = BackendType.OpenAi,
                    Model = "gpt-5-nano"
                }, ["code", "github", "repository", "packages", "npm"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();

        var result = await context
            .ProcessAsync("What is the most used npm package?");

        Console.WriteLine(result.Message.Content);
    }
}