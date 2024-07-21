using MaIN.Models.Rag;

namespace MaIN.Infrastructure.Models;

public class AgentContextDocument
{
    public string Instruction { get; set; }
    public AgentSourceDocument? Source { get; set; }
    public List<string> Steps { get; set; }
    public List<string>? Relations { get; set; }
}