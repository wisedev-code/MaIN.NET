using MaIN.Domain.Entities.Agents.Commands.Base;

namespace MaIN.Domain.Entities.Agents.Commands;

public class RedirectCommand : BaseCommand
{
    public Message Message { get; set; }
    public string RelatedAgentId { get; set; } = null!;
    public OutputTypeOfRedirect SaveAs { get; set; }
}

public enum OutputTypeOfRedirect
{
    AS_Filter,
    AS_Output
}