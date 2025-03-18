using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Dtos.Rag.AgentSource;

public class AgentFileSourceDetailsDto : AgentSourceDetailsBase
{
    public required string Path { get; set; }
}