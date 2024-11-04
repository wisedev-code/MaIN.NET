namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentSource
{
    public object? Details { get; set; }
    public string? AdditionalMessage { get; set; }
    public AgentSourceType Type { get; set; }
}