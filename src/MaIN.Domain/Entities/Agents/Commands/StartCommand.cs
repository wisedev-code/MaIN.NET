using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class StartCommand : BaseCommand
{
    public string? InitialPrompt { get; init; }
}