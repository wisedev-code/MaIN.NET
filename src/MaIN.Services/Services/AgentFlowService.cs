using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class AgentFlowService(IAgentFlowRepository flowRepository, IAgentService agentService) : IAgentFlowService
{
    public async Task<AgentFlow> GetFlowById(string id)
        => (await flowRepository.GetFlowById(id)).ToDomain();

    public async Task<List<AgentFlow>> GetAllFlows()
        => (await flowRepository.GetAllFlows()).Select(x => x.ToDomain()).ToList();

    public async Task<AgentFlow> CreateFlow(AgentFlow flow)
    {
        flow.Id ??= Guid.NewGuid().ToString();
        await flowRepository.AddFlow(flow.ToDocument());
        foreach (var agent in flow.Agents)
        {
            await agentService.CreateAgent(agent);
        }
        
        return flow;
    }

    public async Task DeleteFlow(string id)
    {
        var flow = await flowRepository.GetFlowById(id);
        foreach (var agent in flow.Agents)
        {
            await agentService.DeleteAgent(agent.Id);
        }
        
        await flowRepository.DeleteFlow(id);
    }
}