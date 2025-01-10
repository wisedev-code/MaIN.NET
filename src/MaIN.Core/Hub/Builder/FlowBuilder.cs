using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub;

public class FlowBuilder
{
    private readonly IAgentFlowService _flowService;
    private AgentFlow _flow = new();

    public FlowBuilder(IAgentFlowService flowService)
    {
        _flowService = flowService;
    }

    public FlowBuilder WithName(string name)
    {
        _flow.Name = name;
        return this;
    }

    public FlowBuilder AddAgent(string agentId)
    {
        //_flow.AgentIds.Add(agentId);
        return this;
    }

    public async Task<AgentFlow> CreateAsync()
    {
        return await _flowService.CreateFlow(_flow);
    }
}