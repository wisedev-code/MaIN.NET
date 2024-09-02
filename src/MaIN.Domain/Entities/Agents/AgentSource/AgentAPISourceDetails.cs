namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentApiSourceDetails : AgentSourceDetailsBase
{
    public string Url { get; set; }
    public string Method { get; set; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
}