using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Domain.Repositories;
using System.Collections.Concurrent;

namespace MaIN.Infrastructure.Repositories;

public class DefaultAgentRepository : IAgentRepository
{
    private readonly ConcurrentDictionary<string, Agent> _agents = new();

    public Task<IEnumerable<Agent>> GetAllAgents() => Task.FromResult(_agents.Values.AsEnumerable());

    public Task<Agent?> GetAgentById(string id) => Task.FromResult(_agents.GetValueOrDefault(id));

    public Task AddAgent(Agent agent) =>
        !_agents.TryAdd(agent.Id, agent)
            ? throw new AgentAlreadyExistsException(agent.Id)
            : Task.CompletedTask;

    public Task UpdateAgent(string id, Agent agent) =>
        !_agents.TryUpdate(id, agent, _agents.GetValueOrDefault(id)!)
            ? throw new AgentNotFoundException(id)
            : Task.CompletedTask;

    public Task DeleteAgent(string id) =>
        !_agents.TryRemove(id, out _)
            ? throw new AgentNotFoundException(id)
            : Task.CompletedTask;

    public Task<bool> Exists(string id) => Task.FromResult(_agents.ContainsKey(id));
}
