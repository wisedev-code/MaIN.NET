using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub.Contexts;

public class AgentContext
{
    private readonly IAgentService _agentService;
    private Agent _agent;

    internal AgentContext(IAgentService agentService)
    {
        _agentService = agentService;
        _agent = new Agent
        {
            Id = Guid.NewGuid().ToString(),
            Behaviours = new Dictionary<string, string>()
        };
    }

    internal AgentContext(IAgentService agentService, Agent existingAgent)
    {
        _agentService = agentService;
        _agent = existingAgent;
    }

    public AgentContext WithId(string id)
    {
        _agent.Id = id;
        return this;
    }

    public AgentContext WithName(string name)
    {
        _agent.Name = name;
        return this;
    }

    public AgentContext WithModel(string model)
    {
        _agent.Model = model;
        return this;
    }

    public AgentContext WithInitialPrompt(string prompt)
    {
        _agent.Context.Instruction = prompt;
        return this;
    }

    public AgentContext WithBehaviour(string name, string instruction)
    {
        _agent.Behaviours ??= new Dictionary<string, string>();
        _agent.Behaviours[name] = instruction;
        _agent.CurrentBehaviour = name;
        return this;
    }

    // Creation and Processing
    public async Task<Agent> CreateAsync(bool flow = false)
    {
        return await _agentService.CreateAgent(_agent, flow);
    }

    public async Task<Chat> ProcessAsync(Chat chat, bool translate = false)
    {
        return (await _agentService.Process(chat, _agent.Id, translate))!;
    }

    // Chat Operations
    public async Task<Chat> GetChat()
    {
        return await _agentService.GetChatByAgent(_agent.Id);
    }

    public async Task<Chat> RestartChat()
    {
        return await _agentService.Restart(_agent.Id);
    }

    // Agent Management
    public async Task<List<Agent>> GetAllAgents()
    {
        return await _agentService.GetAgents();
    }

    public async Task Delete()
    {
        await _agentService.DeleteAgent(_agent.Id);
    }

    public async Task<bool> Exists()
    {
        return await _agentService.AgentExists(_agent.Id);
    }

    // Static factory methods
    public static async Task<AgentContext> FromExisting(IAgentService agentService, string agentId)
    {
        var existingAgent = await agentService.GetAgentById(agentId);
        if (existingAgent == null)
            throw new ArgumentException("Agent not found", nameof(agentId));
            
        return new AgentContext(agentService, existingAgent);
    }
}