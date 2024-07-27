namespace MaIN.Models.Rag;

public class AgentContextDto
{
    public string Instruction { get; set; }
    public List<string> Steps { get; set; }
    public List<string>? Relations { get; set; }

}