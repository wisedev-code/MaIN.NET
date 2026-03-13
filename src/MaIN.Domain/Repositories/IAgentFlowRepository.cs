using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Domain.Repositories;

public interface IAgentFlowRepository
{
    Task<IEnumerable<AgentFlow>> GetAllFlows();
    Task<AgentFlow?> GetFlowById(string id);
    Task AddFlow(AgentFlow flow);
    Task UpdateFlow(string id, AgentFlow flow);
    Task DeleteFlow(string id);
}
