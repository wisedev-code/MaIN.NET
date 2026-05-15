using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates <c>.WithAllSkills()</c>: attaches every skill currently in the registry except:
///   - MaIN's bundled ones (web-search, journalist, rag-expert, summarizer, mcp-tool-caller)
///   - skills with Replace placement (e.g. funfact-writer) — they wipe the step pipeline and are exclusive
///
/// In Program.cs the host registers:
///   - services.AddSkillsFromDirectory("./skills")           — folder skills (e.g. code-review, funfact-writer)
///   - services.AddSingleton&lt;IAgentSkillProvider, CalculatorSkill&gt;()  — custom C# skill
///
/// The prompt below intentionally hits BOTH composable skills in one shot:
///   - code-review (folder, .md) inspects the snippet for bugs / style
///   - calculator (custom C# tool) verifies the arithmetic claim made in a comment
///
/// A bundled or Replace-placement skill can be opted in by chaining .WithSkill("name") after .WithAllSkills().
/// </summary>
public class AgentWithAllSkillsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with .WithAllSkills() (code-review folder skill + CalculatorSkill, OpenAi)");
        Console.WriteLine("Skipped: MaIN bundled skills (web-search, journalist, rag-expert, summarizer, mcp-tool-caller).");

        OpenAiExample.Setup();

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithAllSkills()
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("""
                                   Review this code and verify the math in the comment using the calculator tool:

                                   // Net price after 15% discount on $1250 is $1062.5
                                   public decimal ApplyDiscount(decimal price)
                                   {
                                        var discount = price * 0.15;
                                        return price - discount;
                                   }
                                   """);
    }
}
