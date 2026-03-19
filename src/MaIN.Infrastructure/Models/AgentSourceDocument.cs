namespace MaIN.Infrastructure.Models;

internal class AgentSourceDocument
{
    public string? DetailsSerialized { get; init; }
    public AgentSourceTypeDocument Type { get; init; }
    public string? AdditionalMessage { get; init; }
}
