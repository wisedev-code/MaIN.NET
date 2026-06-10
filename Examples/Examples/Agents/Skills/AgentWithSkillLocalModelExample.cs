using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates using an agent with local models (Gemma 3 4B) along with skills.
/// Like AgentWithAllSkillsExample, it leverages .WithAllSkills() to bring in 
/// available local and custom skills (e.g. CodeReview and Calculator).
/// </summary>
public class AgentWithSkillLocalModelExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with .WithAllSkills() (code-review folder skill + CalculatorSkill) running against a Local Model (Gemma3_4b)");
        Console.WriteLine("Skipped: MaIN bundled skills (web-search, journalist, rag-expert, summarizer, mcp-tool-caller).");

        var context = await AIHub.Agent()
            .WithModel(Models.Local.Gemma3_4b)
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
