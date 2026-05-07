using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Skills;

public class SummarizerSkillProvider : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "summarizer",
        Description = "Appends a concise bullet-point summary after answering.",
        Tags = ["summarize", "compression"],
        Priority = 80,
        StepPlacement = SkillStepPlacement.After,
        Steps = ["ANSWER"],
        InstructionFragment = "Always conclude your response with a concise 3-bullet summary."
    };
}
