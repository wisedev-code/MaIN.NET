using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

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
            .WithSkill("code-review") // name provided in name section in SKILL.md file
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