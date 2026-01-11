namespace MaIN.Infrastructure.Models;

public class AgentSourceDocument
{
    public string? DetailsSerialized { get; init; }
    public AgentSourceTypeDocument Type { get; init; }
    public string? AdditionalMessage { get; init; }
}