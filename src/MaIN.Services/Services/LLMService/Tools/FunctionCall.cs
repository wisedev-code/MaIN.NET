namespace MaIN.Services.Services.LLMService.Tools;

public class FunctionCall
{
    public string Name { get; set; } = null!;
    public string Arguments { get; set; } = null!;
}