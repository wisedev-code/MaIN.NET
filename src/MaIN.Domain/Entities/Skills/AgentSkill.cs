using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Domain.Entities.Skills;

public class AgentSkill
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string Version { get; init; } = "1.0.0";
    public List<string> Steps { get; init; } = [];
    public List<SkillToolDefinition> Tools { get; init; } = [];
    public SkillSourceDefinition? Source { get; init; }
    public SkillMcpDefinition? Mcp { get; init; }
    public Dictionary<string, string> Behaviours { get; init; } = [];
    public List<KnowledgeIndexItem> KnowledgeSeed { get; init; } = [];
    public string? InstructionFragment { get; init; }
    public string[] Tags { get; init; } = [];
    public int Priority { get; init; } = 100;
    public SkillStepPlacement StepPlacement { get; init; } = SkillStepPlacement.Before;
}
