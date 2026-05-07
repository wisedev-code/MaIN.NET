using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Skills;

public class McpToolCallerSkillProvider : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "mcp-tool-caller",
        Description = "Adds MCP tool-calling step to the agent pipeline.",
        Tags = ["mcp", "tools"],
        Priority = 5,
        StepPlacement = SkillStepPlacement.Before,
        Steps = ["MCP"]
    };
}
