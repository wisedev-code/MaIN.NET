namespace MaIN.Domain.Entities.Agents;

public class Agent
{
    public required string Id { get; set; }
    public string Name { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Description { get; init; }
    public bool Started { get; set; }
    public bool Flow { get; set; }
    public AgentData? Context { get; set; }
    public string? ChatId { get; set; }
    public int Order { get; set; }
    public Dictionary<string, string>? Behaviours { get; set; }
    public string CurrentBehaviour { get; set; }
}