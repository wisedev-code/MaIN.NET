using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Models.Rag;

public class AgentFileSourceDetailsDto : AgentSourceDetailsBase
{
    public string Path { get; set; }
}