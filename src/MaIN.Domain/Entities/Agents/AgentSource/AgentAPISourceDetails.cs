namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentApiSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public string Url { get; set; }
    public string Method { get; set; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
    public string ResponseType { get; set; }
    public int? ChunkLimit { get; set; }
}