namespace MaIN.Domain.Entities.Agents;

public class AgentData
{
    public string Instruction { get; set; }
    public AgentSource.AgentSource? Source { get; set; }
    public List<string> Steps { get; set; }
    public List<string>? Relations { get; set; }

}