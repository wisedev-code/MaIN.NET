namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFlow
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Agent> Agents { get; set; }
}