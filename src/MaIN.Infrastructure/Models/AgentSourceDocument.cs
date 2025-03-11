namespace MaIN.Models.Rag;

public class AgentSourceDocument
{
    public string? DetailsSerialized { get; init; }
    public AgentSourceTypeDocument Type { get; init; }
    public string? AdditionalMessage { get; init; }
}