using MaIN.Infrastructure.Models;

namespace MaIN.Infrastructure.Repositories.Abstract;

public interface IAgentRepository
{
    Task<IEnumerable<AgentDocument?>> GetAllAgents();
    Task<AgentDocument?> GetAgentById(string id);
    Task AddAgent(AgentDocument? agent);
    Task UpdateAgent(string id, AgentDocument? agent);
    Task DeleteAgent(string id);
}