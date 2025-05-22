using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;

namespace Examples;

public class McpAgentsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpClientExample is running!");
        OpenAiExample.Setup();

        var contextSecond = await AIHub.Agent()
            .WithModel("qwen3:8b")
            .WithInitialPrompt("You are code assistant, Your main role is to review code and give suggestions.")
            .CreateAsync(interactiveResponse: true);
        
        var context = await AIHub.Agent()
            .WithMcpConfig(new Mcp
            {
                Name = "GitHub",
                Arguments = ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    {"GITHUB_PERSONAL_ACCESS_TOKEN", "<your_token>"}
                },
                Command = "docker",
                Model = "gpt-4o-mini"
            })
            .WithModel("gpt-4o-mini")
            .WithSteps(StepBuilder.Instance
                .Mcp()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .CreateAsync();
        
        await context.ProcessAsync("What do you think about example runner in MaIN.NET project?");
    }
}