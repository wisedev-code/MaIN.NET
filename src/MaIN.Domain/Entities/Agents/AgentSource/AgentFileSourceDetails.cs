namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFileSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required string Path { get; init; }
    public required string Name { get; init; }
}