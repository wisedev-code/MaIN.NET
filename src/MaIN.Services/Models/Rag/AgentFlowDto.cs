namespace MaIN.Models.Rag;

public class AgentFlowDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<AgentDto> Agents { get; set; }
}