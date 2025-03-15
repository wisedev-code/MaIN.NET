using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class FetchCommand : BaseCommand
{
    public string? Filter { get; init; }
    public required AgentData Context { get; init; }
}