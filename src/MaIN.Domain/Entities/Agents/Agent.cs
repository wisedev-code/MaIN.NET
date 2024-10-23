namespace MaIN.Domain.Entities.Agents;

public class Agent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public string? Description { get; set; }
    public bool Started { get; set; }
    public AgentContext Context { get; set; }
    public string? ChatId { get; set; }
    public int Order { get; set; }
    public Dictionary<string, string> Behaviours { get; set; }
    public string CurrentBehaviour { get; set; }
}