using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class AnswerCommand : BaseCommand
{
    public bool LastChunk { get; set; }
    public bool TemporaryChat { get; set; }
}