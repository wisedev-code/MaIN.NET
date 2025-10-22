using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Tools;

namespace MaIN.Domain.Entities.Agents;

public class Agent
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Description { get; init; }
    public bool Started { get; set; }
    public bool Flow { get; set; }
    public required AgentData Context { get; init; }
    public string ChatId => string.Empty;
    public int Order { get; set; }
    public BackendType? Backend { get; set; }
    public Dictionary<string, string> Behaviours { get; set; } = [];
    public required string CurrentBehaviour { get; set; }
    public ToolsConfiguration? ToolsConfiguration { get; set; }
}