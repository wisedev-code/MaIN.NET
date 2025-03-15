namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentApiSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required string Url { get; set; }
    public required string Method { get; init; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
    public string? ResponseType { get; init; }
    public int? ChunkLimit { get; init; }
}