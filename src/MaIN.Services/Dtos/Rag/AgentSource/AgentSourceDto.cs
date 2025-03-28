namespace MaIN.Services.Dtos.Rag.AgentSource;

public class AgentSourceDto
{
    public object? Details { get; set; }
    public AgentSourceTypeDto Type { get; set; }
    public string? AdditionalMessage { get; set; }
}