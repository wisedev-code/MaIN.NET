using MaIN.Domain.Entities;

namespace MaIN.Infrastructure.Models;

public class AgentConfigDocument
{
    public string? Instruction { get; init; }
    public AgentSourceDocument? Source { get; init; }
    public List<string>? Steps { get; init; }
    public List<string>? Relations { get; init; }
    public Mcp? McpConfig { get; set; }
}
