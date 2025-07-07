namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFileSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public List<string> Files { get; init; } = [];
    public bool PreProcess { get; init; } = false;
}