using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Models;

namespace Examples.Mcp;

public class McpAgentsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpClientExample is running!");

        AIHub.Extensions.DisableLLamaLogs();
        var contextSecond = await AIHub.Agent()
            .WithModel(Models.Local.QwQ_7b)
            .WithInitialPrompt("Your main role is to provide opinions about facts that you are given in a conversation.")
            .CreateAsync(interactiveResponse: true);

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithMcpConfig(new MaIN.Domain.Entities.Mcp
            {
                Name = "GitHub",
                Arguments = ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    {"GITHUB_PERSONAL_ACCESS_TOKEN", "<YOUR_GITHUB_TOKEN>"}
                },
                Command = "docker",
                Model = Models.OpenAi.Gpt4oMini
            })
            .WithSteps(StepBuilder.Instance
                .Mcp()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .CreateAsync();

        await context.ProcessAsync("What are recently added features in https://github.com/wisedev-code/MaIN.NET (based on recently closed issues)", translate: true);
    }
}
