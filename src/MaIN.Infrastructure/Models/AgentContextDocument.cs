using MaIN.Models.Rag;

namespace MaIN.Infrastructure.Models;

public class AgentContextDocument
{
    public string? Instruction { get; init; }
    public AgentSourceDocument? Source { get; init; }
    public List<string>? Steps { get; init; }
    public List<string>? Relations { get; init; }
}