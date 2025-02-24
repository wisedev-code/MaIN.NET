using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Models.Rag.AgentSource;

public class AgentApiSourceDetailsDto : AgentSourceDetailsBase
{
    public string Url { get; set; }
    public string Method { get; set; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
}