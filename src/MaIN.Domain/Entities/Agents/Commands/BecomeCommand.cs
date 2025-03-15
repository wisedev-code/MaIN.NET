using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class BecomeCommand : BaseCommand
{
    public required string Key { get; set; }
}