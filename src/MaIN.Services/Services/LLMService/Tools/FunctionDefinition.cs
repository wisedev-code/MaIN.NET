namespace MaIN.Services.Services.LLMService.Tools;

public class FunctionDefinition
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public object Parameters { get; set; } = null!;
}