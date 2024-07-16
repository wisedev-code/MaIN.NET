namespace MaIN.Domain.Entities.Agents;

public class AgentContext
{
    public string Instruction { get; set; }
    public AgentSource.AgentSource Source { get; set; }
    public ILookup<string,Delegate> Steps { get; set; }
    public List<AgentRelation>? Relations { get; set; }

}