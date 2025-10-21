namespace MaIN.Domain.Entities.Tools;

public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public FunctionDefinition Function { get; set; } = null!;
    public Func<string, Task<string>>? Execute { get; set; }
}