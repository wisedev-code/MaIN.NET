using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class RedirectCommand : BaseCommand
{
    public string? RelatedAgentId { get; set; }
}