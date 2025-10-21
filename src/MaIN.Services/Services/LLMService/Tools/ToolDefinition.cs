namespace MaIN.Services.Services.LLMService.Tools;

public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public FunctionDefinition Function { get; set; } = null!;
}