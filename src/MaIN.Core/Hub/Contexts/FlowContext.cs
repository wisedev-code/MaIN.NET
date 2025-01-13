using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub.Contexts;

public class FlowContext
{
    private readonly IAgentFlowService _flowService;
    private AgentFlow _flow;

    internal FlowContext(IAgentFlowService flowService)
    {
        _flowService = flowService;
        _flow = new AgentFlow
        {
            Id = Guid.NewGuid().ToString(),
            Agents = new List<Agent>(),
        };
    }

    internal FlowContext(IAgentFlowService flowService, AgentFlow existingFlow)
    {
        _flowService = flowService;
        _flow = existingFlow;
    }

    public FlowContext WithId(string id)
    {
        _flow.Id = id;
        return this;
    }

    public FlowContext WithName(string name)
    {
        _flow.Name = name;
        return this;
    }

    public FlowContext AddAgent(Agent agent)
    {
        _flow.Agents.Add(agent);
        return this;
    }
    
    // Creation and Management
    public async Task<AgentFlow> CreateAsync()
    {
        return await _flowService.CreateFlow(_flow);
    }

    public async Task Delete()
    {
        if (_flow.Id == null)
            throw new InvalidOperationException("Flow has not been created yet.");
            
        await _flowService.DeleteFlow(_flow.Id);
    }

    // Retrieval Methods
    public async Task<AgentFlow> GetCurrentFlow()
    {
        if (_flow.Id == null)
            throw new InvalidOperationException("Flow has not been created yet.");
            
        return await _flowService.GetFlowById(_flow.Id);
    }

    public async Task<List<AgentFlow>> GetAllFlows()
    {
        return await _flowService.GetAllFlows();
    }

    // Static factory methods
    public static async Task<FlowContext> FromExisting(IAgentFlowService flowService, string flowId)
    {
        var existingFlow = await flowService.GetFlowById(flowId);
        if (existingFlow == null)
            throw new ArgumentException("Flow not found", nameof(flowId));
            
        return new FlowContext(flowService, existingFlow);
    }
}