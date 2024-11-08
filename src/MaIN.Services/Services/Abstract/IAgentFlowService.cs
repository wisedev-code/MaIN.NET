using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Services.Abstract;

public interface IAgentFlowService
{
    Task<AgentFlow> GetFlowById(string id);
    Task<List<AgentFlow>> GetAllFlows();
    Task<AgentFlow> CreateFlow(AgentFlow toDomain);
    Task DeleteFlow(string id);
}