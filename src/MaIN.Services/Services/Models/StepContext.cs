using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;

namespace MaIN.Services.Services.Models;

public class StepContext
{
    public required AgentDocument Agent { get; init; }
    public required Chat Chat { get; init; }
    public required Message RedirectMessage { get; init; }
    public required List<string> TagsToReplaceWithFilter { get; init; }
    public required string[] Arguments { get; init; }
    public Mcp? McpConfig { get; init; }
    public required Func<string, string, string?, string, Task> NotifyProgress { get; init; }
    public required Func<Chat, Task> UpdateChat { get; init; }
    public required string StepName { get; init; }
}