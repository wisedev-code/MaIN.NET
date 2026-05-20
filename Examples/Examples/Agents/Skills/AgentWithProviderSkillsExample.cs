using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates a SKILL.md bundle delegated to a cloud provider's native Skills API.
/// On startup MaIN uploads every uploadable bundle (file-only skills under
/// MaINSettings.SkillsDirectory) to OpenAI and Anthropic. At agent-creation time,
/// SkillComposer detects the cloud backend and routes the skill as a
/// ProviderSkillReference instead of inlining its instruction fragment / tool schemas,
/// which lowers input tokens on every subsequent request.
/// </summary>
public class AgentWithProviderSkillsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with provider-side skill (OpenAI Responses API)");

        OpenAiExample.Setup();

        // Assumes a SKILL.md bundle named e.g. "report-writer" lives under the configured
        // SkillsDirectory and got auto-uploaded to OpenAI on startup. If the upload failed
        // (no key, bundle not on disk), SkillComposer falls back to local composition.
        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("report-writer")
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("Draft a one-page weekly report on AI tooling.");
    }
}
