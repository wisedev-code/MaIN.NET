namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentTextSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public string Text { get; set; }
}