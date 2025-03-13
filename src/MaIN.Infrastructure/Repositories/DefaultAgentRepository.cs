using System.Collections.Concurrent;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories;

public class DefaultAgentRepository : IAgentRepository
{
    private readonly ConcurrentDictionary<string, AgentDocument> _agents = new();

    public async Task<IEnumerable<AgentDocument>> GetAllAgents() =>
        await Task.FromResult(_agents.Values);

    public async Task<AgentDocument?> GetAgentById(string id) =>
        await Task.FromResult(_agents.GetValueOrDefault(id));

    public async Task AddAgent(AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));
            
        if (!_agents.TryAdd(agent.Id, agent))
            throw new InvalidOperationException($"Agent with ID {agent.Id} already exists.");
            
        await Task.CompletedTask;
    }

    public async Task UpdateAgent(string id, AgentDocument agent)
    {
        if (!_agents.TryUpdate(id, agent, _agents.GetValueOrDefault(id)!))
            throw new KeyNotFoundException($"Agent with ID {id} not found.");
            
        await Task.CompletedTask;
    }

    public async Task DeleteAgent(string id)
    {
        if (!_agents.TryRemove(id, out _))
            throw new KeyNotFoundException($"Agent with ID {id} not found.");
            
        await Task.CompletedTask;
    }

    public async Task<bool> Exists(string id) =>
        await Task.FromResult(_agents.ContainsKey(id));
}