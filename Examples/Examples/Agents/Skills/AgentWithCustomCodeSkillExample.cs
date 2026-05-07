using System.Data;
using System.Text.Json;
using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates a custom code-based skill defined in the Examples project.
/// CalculatorSkill implements IAgentSkillProvider and is registered in Program.cs via
/// services.AddSingleton&lt;IAgentSkillProvider, CalculatorSkill&gt;(). The SkillRegistry
/// picks it up automatically at startup — no manual Register() call needed here.
/// This is the key difference from file-based skills: code skills can execute C# functions as tools.
/// </summary>
public class AgentWithCustomCodeSkillExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with custom code-based skill (CalculatorSkill, OpenAi)");

        OpenAiExample.Setup();

        // CalculatorSkill is registered in Program.cs via DI (services.AddSingleton<IAgentSkillProvider, CalculatorSkill>();) and is already in the registry.
        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("calculator")
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync(
            "A shop sells 3 items: apple $1.25, banana $0.80, cherry $3.40. " +
            "I buy 4 apples, 7 bananas and 2 cherries. What is the total cost? " +
            "Also, if I pay with $30, what is my change?");
    }
}

/// <summary>
/// Custom code-based skill defined in the Examples project.
/// Gives the agent a "calculate" tool backed by a real C# function — something
/// .md file-based skills cannot do (they have no executable code).
/// </summary>
public class CalculatorSkill : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "calculator",
        Description = "Gives the agent a precise calculation tool. Use when the prompt involves arithmetic.",
        Version = "1.0.0",
        Steps = ["ANSWER"],
        StepPlacement = SkillStepPlacement.Before,
        Priority = 20,
        Tags = ["math", "tools", "calculator"],
        Tools =
        [
            new SkillToolDefinition
            {
                Name = "calculate",
                Description = "Evaluates a mathematical expression and returns the result. " +
                              "Supports +, -, *, /, %, ^ and parentheses.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        expression = new
                        {
                            type = "string",
                            description = "The math expression to evaluate, e.g. \"(12 + 8) * 3 / 4\""
                        }
                    },
                    required = new[] { "expression" }
                },
                Execute = async args =>
                {
                    await Task.CompletedTask;
                    try
                    {
                        var doc = JsonDocument.Parse(args);
                        var expression = doc.RootElement.GetProperty("expression").GetString() ?? "";
                        var result = new DataTable().Compute(expression, null);
                        return $"{result}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error: {ex.Message}";
                    }
                }
            }
        ],
        InstructionFragment =
            "You have access to a precise calculator tool. " +
            "For any arithmetic — no matter how simple — always call the calculate tool instead of computing mentally. " +
            "Show the expression you used and the result."
    };
}