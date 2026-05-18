using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

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
        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("web-search")
            .WithSkill("file-journalist")  // loaded from ./skills/file-journalist.md
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Provide today's newsletter.");
    }
}