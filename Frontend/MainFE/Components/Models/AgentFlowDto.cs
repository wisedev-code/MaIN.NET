using MaIN.Models.Rag;

namespace MainFE.Components.Models;

public class AgentFlowDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<AgentDto> Agents { get; set; }
    public string Description { get; set; }
}