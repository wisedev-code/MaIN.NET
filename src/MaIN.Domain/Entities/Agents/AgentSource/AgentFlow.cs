namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentFlow
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public List<Agent> Agents { get; init; } = [];
    public string? Description { get; set; }
}