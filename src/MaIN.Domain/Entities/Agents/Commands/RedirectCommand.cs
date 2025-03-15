using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class RedirectCommand : BaseCommand
{
    public required Message Message { get; init; }
    public required string RelatedAgentId { get; init; }
    public OutputTypeOfRedirect SaveAs { get; init; }
    public string? Filter { get; init; }
}

public enum OutputTypeOfRedirect
{
    AS_Filter,
    AS_Output
}