using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class AgentFlowService(IAgentFlowRepository flowRepository) : IAgentFlowService
{
    public async Task<AgentFlow> GetFlowById(string id)
        => (await flowRepository.GetFlowById(id)).ToDomain();

    public async Task<AgentFlow> CreateFlow(AgentFlow flow)
    {
        await flowRepository.AddFlow(flow.ToDocument());
        return flow;
    }

    public async Task DeleteFlow(string id)
        => await flowRepository.DeleteFlow(id);
}