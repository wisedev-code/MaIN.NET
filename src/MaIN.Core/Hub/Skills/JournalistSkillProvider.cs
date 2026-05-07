using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Skills;

public class JournalistSkillProvider : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "journalist",
        Description = "Applies a journalist persona that writes newsletters from fetched data.",
        Tags = ["persona", "journalism"],
        Priority = 50,
        StepPlacement = SkillStepPlacement.Before,
        Steps = [$"BECOME+Journalist", "ANSWER"],
        Behaviours = new Dictionary<string, string>
        {
            ["Journalist"] = $"Based on data provided in chat, write a newsletter. Date: {DateTime.UtcNow:D}. Be concise and factual."
        }
    };
}
