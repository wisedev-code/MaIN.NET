using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates skills applied via code — built-in "web-search" + "journalist".
/// Equivalent to AgentWithWebDataSourceOpenAiExample but with 2 lines instead of 15.
/// </summary>
public class AgentWithSkillsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with registered skills (code-based, OpenAi)");

        OpenAiExample.Setup();

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("web-search")   // FETCH_DATA step + BBC source
            .WithSkill("journalist")   // BECOME+Journalist + ANSWER steps + behaviour
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Provide today's newsletter");
    }
}