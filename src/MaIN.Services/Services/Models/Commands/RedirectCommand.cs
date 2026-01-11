using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Models.Commands;

public class RedirectCommand : BaseCommand, ICommand<Message?>
{
    public required Message Message { get; init; }
    public required string RelatedAgentId { get; init; }
    public OutputTypeOfRedirect SaveAs { get; init; }
    public string? Filter { get; init; }
    public string? CommandName => "REDIRECT";
}

public enum OutputTypeOfRedirect
{
    AS_Filter = 0,
    AS_Output = 1
}