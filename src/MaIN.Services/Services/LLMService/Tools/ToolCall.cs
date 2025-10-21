namespace MaIN.Services.Services.LLMService.Tools;

public class ToolCall
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "function";
    public FunctionCall Function { get; set; } = null!;
}