using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Models.Rag.AgentSource;

public class AgentFileSourceDetailsDto : AgentSourceDetailsBase
{
    public string Path { get; set; }
}