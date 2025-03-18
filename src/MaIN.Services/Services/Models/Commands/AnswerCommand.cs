using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Models.Commands;

public class AnswerCommand : BaseCommand, ICommand<Message?>
{
    public bool LastChunk { get; set; }
    public bool TemporaryChat { get; set; }
    public bool UseMemory { get; init; }
    public string CommandName => "ANSWER";
}