using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Dtos.Rag.AgentSource;

public class AgentApiSourceDetailsDto : AgentSourceDetailsBase
{
    public required string Url { get; set; }
    public required string Method { get; set; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
}