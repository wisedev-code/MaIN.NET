using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Domain.Entities.Skills;

public enum SkillStepPlacement { Before, After, Replace }

public class SkillToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required object Parameters { get; init; }
    public Func<string, Task<string>>? Execute { get; init; }
    public string? ToolChoice { get; init; }
}

public class SkillSourceDefinition
{
    public required IAgentSource Details { get; init; }
    public required AgentSourceType Type { get; init; }
}

public class SkillMcpDefinition
{
    public required string Command { get; init; }
    public required List<string> Arguments { get; init; }
    public Dictionary<string, string> Environment { get; init; } = [];
    public Dictionary<string, string> Properties { get; init; } = [];
}
