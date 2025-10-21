using MaIN.Domain.Models;
using MaIN.Services.Services.LLMService.Tools;

namespace MaIN.Services.Services.LLMService;

public class ChatRequestOptions
{
    public bool InteractiveUpdates { get; set; }
    public bool CreateSession { get; set; }
    public bool SaveConv { get; set; } = true;
    public Func<LLMTokenValue, Task>? TokenCallback { get; set; }
    public List<ToolDefinition>? Tools { get; set; }
    public string? ToolChoice { get; set; }
}