using MaIN.Domain.Entities.Agents;

namespace MaIN.Infrastructure.Repositories.Abstract;

public interface IAgentRepository
{
    Task<IEnumerable<Agent>> GetAllAgents();
    Task<Agent?> GetAgentById(string id);
    Task AddAgent(Agent agent);
    Task UpdateAgent(string id, Agent agent);
    Task DeleteAgent(string id);
    Task<bool> Exists(string id);
}
