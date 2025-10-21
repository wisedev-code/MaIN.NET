using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;

namespace Examples.Mcp;

public class AgentWithKnowledgeMcpExample : IExample
{
    public async Task Start()
    {
        //Note: to run this example that uses 3 different Ai providers. You have to assign api keys for those providers in ENV variables or in appsettings
        //Note: to run this example, you should do 'gh auth login' to give octocode mcp server access to github CLI
        Console.WriteLine("Agent with knowledge base example MCP sources");

        AIHub.Extensions.DisableLLamaLogs();
        var context = await AIHub.Agent()
            .WithBackend(BackendType.OpenAi)
            .WithModel("gpt-4.1-mini")
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddMcp(new MaIN.Domain.Entities.Mcp
                {
                    Name = "ExaDeepSearch",
                    Arguments = ["-y", "exa-mcp-server"],
                    Command = "npx",
                    EnvironmentVariables = {{"EXA_API_KEY","7e16b939-263f-40cc-9880-6948e104fa42"}},
                    Backend = BackendType.Gemini,
                    Model = "gemini-2.0-flash"
                }, ["search", "browser", "web access", "research"])
                .AddMcp(new MaIN.Domain.Entities.Mcp
                {
                    Name = "FileSystem",
                    Command = "npx",
                    Arguments = ["-y",
                        "@modelcontextprotocol/server-filesystem",
                        "/Users/pstach/WiseDev", //Align paths to fit your system
                        ], //Align paths to fit your system
                    Backend = BackendType.GroqCloud,
                    Model = "openai/gpt-oss-20b"
                }, ["filesystem", "file operations", "read write", "disk search"])
                .AddMcp(new MaIN.Domain.Entities.Mcp
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

        Console.WriteLine("Agent ready! Type 'exit' to quit.\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("You: ");
            Console.ResetColor();
            
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "exit") break;
            if (string.IsNullOrWhiteSpace(input)) continue;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Agent: ");
            
            var result = await context.ProcessAsync(input);
            Console.WriteLine(result.Message.Content);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
