
namespace Examples;

using MaIN.Core.Agent;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Models;

internal class AgentWithLocalSkillExample
{
    internal static async Task Main(string[] args)
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        var hub = AIHub.Builder.WithOpenAI(openAIKey).Build();

        var skill = hub.CreateSkill(
            "Examples/Skills/Skills/local-skill.md",
            "local-skill");

        var agent = hub.CreateAgent(
                "local-skill-agent",
                "You are a helpful agent that can use skills.")
            .WithSkill(skill)
            .Build();

        var response = await agent.GetStreamingResponse("What is the capital of France?");

        await foreach (var part in response)
        {
            Console.Write(part);
        }
    }
}
