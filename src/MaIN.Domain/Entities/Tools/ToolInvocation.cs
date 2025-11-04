namespace MaIN.Domain.Entities.Tools;

public class ToolInvocation
{
    public string ToolName { get; set; }
    public string Arguments { get; set; } = null!;
    public bool Done { get; set; } = false;
}