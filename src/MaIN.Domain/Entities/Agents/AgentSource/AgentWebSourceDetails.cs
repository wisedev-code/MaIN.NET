namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentWebSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required string Url { get; init; }
}