namespace MaIN.Models.Rag;

public class AgentDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public string? Description { get; set; }
    public bool Started { get; set; }
    public AgentContextDto Context { get; set; }
}