using System.Collections.Concurrent;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Exceptions.Flows;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories;

public class DefaultAgentFlowRepository : IAgentFlowRepository
{
    private readonly ConcurrentDictionary<string, AgentFlowDocument> _flows = new();

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows() =>
        await Task.FromResult(_flows.Values);

    public async Task<AgentFlowDocument?> GetFlowById(string id) =>
        (await Task.FromResult(_flows.GetValueOrDefault(id)))!;

    public async Task AddFlow(AgentFlowDocument flow)
    {
        if (!_flows.TryAdd(flow.Id, flow))
            throw new FlowAlreadyExistsException(flow.Id);
            
        await Task.CompletedTask;
    }

    public async Task UpdateFlow(string id, AgentFlowDocument flow)
    {
        if (!_flows.TryUpdate(id, flow, _flows.GetValueOrDefault(id)!))
            throw new KeyNotFoundException($"Flow with ID {id} not found.");
            
        await Task.CompletedTask;
    }

    public async Task DeleteFlow(string id)
    {
        if (!_flows.TryRemove(id, out _))
            throw new KeyNotFoundException($"Flow with ID {id} not found.");
            
        await Task.CompletedTask;
    }
}