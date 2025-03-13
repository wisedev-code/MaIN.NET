namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentTextSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required string Text { get; set; }
}