using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class FetchCommand : BaseCommand
{
    public string? Filter { get; set; }
    public AgentContext Context { get; set; }
}