using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates a skill loaded from a .md file in ./skills/ folder.
/// Drop any .md skill file there and it's auto-picked up on startup.
///
/// This example also opts into RequireNativeSkillsApi=true — SkillComposer throws
/// SkillNotSupportedException if any uploadable skill can't be routed through the
/// provider's native Skills API. Use a model that supports Skills (e.g. gpt-5.5) to succeed.
/// </summary>
public class AgentWithFileSkillExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with file-based skill (OpenAi, RequireNativeSkillsApi=true)");
        Console.WriteLine("Looks for skills in ./skills/ directory...");

        MaINBootstrapper.Initialize(configureSettings: options =>
        {
            options.BackendType = BackendType.OpenAi;
            options.OpenAiKey = "<YOUR_OPENAI_KEY>";
            options.SkillUpload.RequireNativeSkillsApi = true;
        });

        // gpt-5.5 supports the native Skills API. Swap to gpt-4o-mini to see SkillNotSupportedException in action.
        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt5_5)
            .WithSkill("web-search")
            .WithSkill("file-journalist")
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Provide today's newsletter.");
    }
}