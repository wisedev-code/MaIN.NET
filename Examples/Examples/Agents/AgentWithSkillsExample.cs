using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents;

/// <summary>
/// Demonstrates skills applied via code — built-in "web-search" + "journalist".
/// Equivalent to AgentWithWebDataSourceOpenAiExample but with 2 lines instead of 15.
/// </summary>
public class AgentWithSkillsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with skills (code-based, OpenAi)");

        OpenAiExample.Setup();

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("web-search")   // FETCH_DATA step + BBC source
            .WithSkill("journalist")   // BECOME+Journalist + ANSWER steps + behaviour
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Provide today's newsletter");
    }
}

/// <summary>
/// Demonstrates a skill loaded from a .md file in ./skills/ folder.
/// Drop any .md skill file there and it's auto-picked up on startup.
/// </summary>
public class AgentWithFileSkillExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with file-based skill (OpenAi)");
        Console.WriteLine("Looks for skills in ./skills/ directory...");

        OpenAiExample.Setup();

        // Skills from ./skills/*.md are auto-loaded if SkillsDirectory is configured,
        // OR you can call AddSkillsFromDirectory() in startup.
        // This example assumes ./skills/journalist.md exists (created alongside examples).
        //var context = await AIHub.Agent()
        //    .WithModel(Models.OpenAi.Gpt4oMini)
        //    .WithSkill("web-search")
        //    .WithSkill("file-journalist")  // loaded from ./skills/file-journalist.md
        //    .CreateAsync(interactiveResponse: true);

        //await context.ProcessAsync("Provide today's newsletter");

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("caveman")
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Tell me facts about killer whale.");

    }
}
