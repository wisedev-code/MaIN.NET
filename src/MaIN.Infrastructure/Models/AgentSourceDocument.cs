namespace MaIN.Models.Rag;

public class AgentSourceDocument
{
    public string? DetailsSerialized { get; set; }
    public AgentSourceTypeDocument Type { get; set; }
    public string? AdditionalMessage { get; set; }
}