namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentWebSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public string Url { get; set; }
}