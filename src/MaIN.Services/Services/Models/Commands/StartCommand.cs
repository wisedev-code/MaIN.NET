using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Models.Commands;

public class StartCommand : BaseCommand, ICommand<Message?>
{
    public string? InitialPrompt { get; init; }
    public string CommandName => "START";
}