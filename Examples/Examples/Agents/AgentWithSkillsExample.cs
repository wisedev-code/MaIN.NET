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
        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("web-search")
            .WithSkill("file-journalist")  // loaded from ./skills/file-journalist.md
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Provide today's newsletter for poland");
    }
}

/// <summary>
/// Demonstrates a folder-based skill: skills/code-review/SKILL.md is the entrypoint,
/// prompts/ and examples/ subdirectories are loaded via the "includes" frontmatter key.
/// </summary>
public class AgentWithFolderSkillExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with folder-based skill (code-review, OpenAi)");
        Console.WriteLine("Skill loaded from: ./skills/code-review/SKILL.md");

        OpenAiExample.Setup();

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("code-review")
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("""
                                   Review this code:
                                   public List<string> GetNames(List<User> users) 
                                   {
                                        List<string> names = new List<string>();
                                        for (int i = 0; i < users.Count; i++) 
                                        {
                                            names.Add(users[i].Name);
                                        }
                                        return names;
                                   }
                                   """);
    }
}
