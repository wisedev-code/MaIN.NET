using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub;

public class AgentBuilder
{
    private readonly IAgentService _agentService;
    private Agent _agent = new();

    public AgentBuilder(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public AgentBuilder WithId(string id)
    {
        _agent.Id = id;
        return this;
    }

    public AgentBuilder WithInitialPrompt(string prompt)
    {
        //_agent.InitialPrompt = prompt;
        return this;
    }

    public async Task<Agent> CreateAsync(bool flow = false)
    {
        return await _agentService.CreateAgent(_agent, flow);
    }

    public async Task<Chat> ProcessAsync(Chat chat, bool translate = false)
    {
        return (await _agentService.Process(chat, _agent.Id, translate))!;
    }
}