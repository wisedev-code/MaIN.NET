namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFileSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public string Path { get; set; }
}