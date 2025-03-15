using MaIN.Infrastructure.Models;

namespace MaIN.Infrastructure.Repositories.Abstract;

public interface IAgentFlowRepository
{
    Task<IEnumerable<AgentFlowDocument>> GetAllFlows();
    Task<AgentFlowDocument?> GetFlowById(string id);
    Task AddFlow(AgentFlowDocument flow);
    Task UpdateFlow(string id, AgentFlowDocument flow);
    Task DeleteFlow(string id);
}