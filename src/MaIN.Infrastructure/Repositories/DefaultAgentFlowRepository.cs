using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Exceptions.Flows;
using MaIN.Domain.Repositories;
using System.Collections.Concurrent;

namespace MaIN.Infrastructure.Repositories;

public class DefaultAgentFlowRepository : IAgentFlowRepository
{
    private readonly ConcurrentDictionary<string, AgentFlow> _flows = new();

    public Task<IEnumerable<AgentFlow>> GetAllFlows() => Task.FromResult(_flows.Values.AsEnumerable());
    public Task<AgentFlow?> GetFlowById(string id) => Task.FromResult(_flows.GetValueOrDefault(id));
    public Task AddFlow(AgentFlow flow) =>
        !_flows.TryAdd(flow.Id!, flow)
            ? throw new FlowAlreadyExistsException(flow.Id!)
            : Task.CompletedTask;

    public Task UpdateFlow(string id, AgentFlow flow) =>
        !_flows.TryUpdate(id, flow, _flows.GetValueOrDefault(id)!)
            ? throw new KeyNotFoundException($"Flow with ID {id} not found.")
            : Task.CompletedTask;

    public Task DeleteFlow(string id) =>
        !_flows.TryRemove(id, out _)
            ? throw new KeyNotFoundException($"Flow with ID {id} not found.")
            : Task.CompletedTask;
}
