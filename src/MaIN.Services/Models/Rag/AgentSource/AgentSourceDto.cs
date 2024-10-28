namespace MaIN.Models.Rag;

public class AgentSourceDto
{
    public object? Details { get; set; }
    public AgentSourceTypeDto Type { get; set; }
    public string? AdditionalMessage { get; set; }
}