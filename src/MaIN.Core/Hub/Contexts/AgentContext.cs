using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts;

public class AgentContext
{
    private readonly IAgentService _agentService;
    private InferenceParams? _inferenceParams;
    private Agent _agent;

    internal AgentContext(IAgentService agentService)
    {
        _agentService = agentService;
        _agent = new Agent
        {
            Id = Guid.NewGuid().ToString(),
            Behaviours = new Dictionary<string, string>(),
            Name = $"Agent-{Guid.NewGuid()}",
            Description = "Agent created by MaIN",
            CurrentBehaviour = "Default",
            Flow = false,
            Context = new AgentData()
            {
                Instruction = "Hello, I'm your personal assistant. How can I assist you today?",
                Relations = [],
                Source = null,
                Steps = ["ANSWER"]
            }
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

    public string GetAgentId() => _agent.Id;
    
    public Agent GetAgent() => _agent;
    
    public AgentContext WithOrder(int order)
    {
        _agent.Order = order;
        return this;
    }
    public AgentContext WithSource(IAgentSource source, AgentSourceType type)
    {
        _agent.Context.Source = new AgentSource()
        {
            Details = source,
            Type = type
        };
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

    public AgentContext WithInferenceParams(InferenceParams inferenceParams)
    {
        _inferenceParams = inferenceParams;
        return this;
    }
    
    public AgentContext WithCustomModel(string model, string path)
    {
        KnownModels.AddModel(model, path);
        _agent.Model = model;
        return this;
    }

    
    public AgentContext WithInitialPrompt(string prompt)
    {
        _agent.Context.Instruction = prompt;
        return this;
    }

    public AgentContext WithSteps(List<string>? steps)
    {
        _agent.Context.Steps = steps;
        return this;
    }
    
    public AgentContext WithBehaviour(string name, string instruction)
    {
        _agent.Behaviours ??= new Dictionary<string, string>();
        _agent.Behaviours[name] = instruction;
        _agent.CurrentBehaviour = name;
        return this;
    }

    public async Task<AgentContext> CreateAsync(bool flow = false, bool interactiveResponse = false)
    {
        await _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams);
        return this;
    }
    
    public AgentContext Create(bool flow = false, bool interactiveResponse = false)
    {
        _ = _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams).Result;
        return this;
    }
    
    public async Task<ChatResult> ProcessAsync(Chat chat, bool translate = false)
    {
        var result = await _agentService.Process(chat, _agent.Id, translate);
        var message = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = message,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(string message, bool translate = false)
    {
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(new Message()
        {
            Content = message,
            Role = "User",
            Time = DateTime.Now
        });
        var result = await _agentService.Process(chat, _agent.Id, translate);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(Message message, bool translate = false)
    {
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(message);
        var result = await _agentService.Process(chat, _agent.Id, translate);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public async Task<Chat> GetChat()
    {
        return await _agentService.GetChatByAgent(_agent.Id);
    }

    public async Task<Chat> RestartChat()
    {
        return await _agentService.Restart(_agent.Id);
    }

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

    public static async Task<AgentContext> FromExisting(IAgentService agentService, string agentId)
    {
        var existingAgent = await agentService.GetAgentById(agentId);
        if (existingAgent == null)
            throw new ArgumentException("Agent not found", nameof(agentId));
            
        return new AgentContext(agentService, existingAgent);
    }
}

public static class AgentExtensions
{
    public static async Task<ChatResult> ProcessAsync(
        this Task<AgentContext> agentTask, 
        string message, 
        bool translate = false)
    {
        var agent = await agentTask;
        return await agent.ProcessAsync(message, translate);
    }
}