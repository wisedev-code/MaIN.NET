namespace MaIN.Domain.Entities.Agents;

public class Agent
{
    public required string Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Model { get; set; } = null!;
    public string? Description { get; init; }
    public bool Started { get; set; }
    public bool Flow { get; set; }
    public required AgentData Context { get; init; }
    public string? ChatId { get; init; }
    public int Order { get; set; }
    public Dictionary<string, string>? Behaviours { get; set; }
    public required string CurrentBehaviour { get; set; }
}