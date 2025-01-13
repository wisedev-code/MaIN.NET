using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class FetchCommand : BaseCommand
{
    public string? Filter { get; set; }
    public AgentData Context { get; set; }
}