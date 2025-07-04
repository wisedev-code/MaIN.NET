namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFileSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required Dictionary<string, string> Files { get; init; } = new();
    public bool PreProcess { get; init; } = false;
}