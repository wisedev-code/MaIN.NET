using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts;

public sealed class AgentContext : IAgentBuilderEntryPoint, IAgentConfigurationBuilder, IAgentContextExecutor
{
    private readonly IAgentService _agentService;
    private readonly ISkillRegistry? _skillRegistry;
    private readonly ISkillComposer? _skillComposer;
    private readonly List<string> _pendingSkillNames = [];
    private readonly List<AgentSkill> _pendingInlineSkills = [];
    private bool _allSkillsApplied;
    private IBackendInferenceParams? _inferenceParams;
    private MemoryParams? _memoryParams;
    private bool _disableCache;
    private bool _ensureModelDownloaded;
    private readonly Agent _agent;
    internal Knowledge? _knowledge;

    internal AgentContext(IAgentService agentService, ISkillRegistry skillRegistry, ISkillComposer skillComposer)
    {
        _agentService = agentService;
        _skillRegistry = skillRegistry;
        _skillComposer = skillComposer;
        _agent = new Agent
        {
            Id = Guid.NewGuid().ToString(),
            Behaviours = [],
            Name = $"Agent-{Guid.NewGuid()}",
            Description = "Agent created by MaIN",
            CurrentBehaviour = "Default",
            Flow = false,
            Config = new AgentConfig()
            {
                Instruction = "Hello, I'm your personal assistant. How can I assist you today?",
                Relations = [],
                Source = null,
                Steps = ["ANSWER"]
            }
        };
    }

    internal AgentContext(IAgentService agentService, Agent existingAgent, ISkillRegistry? skillRegistry, ISkillComposer? skillComposer)
    {
        _agentService = agentService;
        _agent = existingAgent;
        _skillRegistry = skillRegistry;
        _skillComposer = skillComposer;
    }

    public IAgentConfigurationBuilder WithSkill(string skillName)
    {
        _pendingSkillNames.Add(skillName);
        return this;
    }

    public IAgentConfigurationBuilder WithSkills(params string[] skillNames)
    {
        _pendingSkillNames.AddRange(skillNames);
        return this;
    }

    public IAgentConfigurationBuilder WithSkill(AgentSkill skill)
    {
        _pendingInlineSkills.Add(skill);
        return this;
    }

    public IAgentConfigurationBuilder WithAllSkills()
    {
        if (_skillRegistry is null) return this;
        if (_allSkillsApplied) return this;
        _allSkillsApplied = true;

        foreach (var skill in _skillRegistry.GetAllExcludingBuiltIn())
        {
            if (skill.StepPlacement == SkillStepPlacement.Replace)
                continue;

            _pendingInlineSkills.Add(skill);
        }

        return this;
    }

    // --- IAgentActions ---
    public string GetAgentId() => _agent.Id;
    public Agent GetAgent() => _agent;
    public Knowledge? GetKnowledge() => _knowledge;
    public async Task<Chat> GetChat() => await _agentService.GetChatByAgent(_agent.Id);
    public async Task<Chat> RestartChat() => await _agentService.Restart(_agent.Id);
    public async Task<List<Agent>> GetAllAgents() => await _agentService.GetAgents();
    public async Task<Agent?> GetAgentById(string id) => await _agentService.GetAgentById(id);
    public async Task Delete() => await _agentService.DeleteAgent(_agent.Id);
    public async Task<bool> Exists() => await _agentService.AgentExists(_agent.Id);

    public IAgentConfigurationBuilder WithModel(string modelId)
    {
        if (!ModelRegistry.Exists(modelId))
        {
            throw new AgentModelNotAvailableException(_agent.Id, modelId);
        }

        _agent.Model = modelId;
        return this;
    }

    public async Task<IAgentContextExecutor> FromExisting(string agentId)
    {
        var existingAgent = await _agentService.GetAgentById(agentId) ?? throw new AgentNotFoundException(agentId);

        var context = new AgentContext(_agentService, existingAgent, _skillRegistry, _skillComposer);
        context.LoadExistingKnowledgeIfExists();
        return context;
    }

    public IAgentConfigurationBuilder WithInitialPrompt(string prompt)
    {
        _agent.Config.Instruction = prompt;
        return this;
    }

    public IAgentConfigurationBuilder WithId(string id)
    {
        _agent.Id = id;
        return this;
    }

    public IAgentConfigurationBuilder WithOrder(int order)
    {
        _agent.Order = order;
        return this;
    }

    public IAgentConfigurationBuilder DisableCache()
    {
        _disableCache = true;
        return this;
    }

    public IAgentConfigurationBuilder EnsureModelDownloaded()
    {
        _ensureModelDownloaded = true;
        return this;
    }

    public IAgentConfigurationBuilder WithSource(IAgentSource source, AgentSourceType type)
    {
        _agent.Config.Source = new AgentSource()
        {
            Details = source,
            Type = type
        };
        return this;
    }

    public IAgentConfigurationBuilder WithName(string name)
    {
        _agent.Name = name;
        return this;
    }

    public IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig)
    {
        if (mcpConfig.Backend is null && ModelRegistry.Exists(_agent.Model))
        {
            mcpConfig.Backend = ModelRegistry.GetById(_agent.Model).Backend;
        }
        _agent.Config.McpConfig = mcpConfig;
        return this;
    }

    public IAgentConfigurationBuilder WithInferenceParams(IBackendInferenceParams inferenceParams)
    {
        _inferenceParams = inferenceParams;
        return this;
    }

    public IAgentConfigurationBuilder WithMemoryParams(MemoryParams memoryParams)
    {
        _memoryParams = memoryParams;
        return this;
    }

    public IAgentConfigurationBuilder WithSteps(List<string>? steps)
    {
        _agent.Config.Steps = steps;
        return this;
    }

    public IAgentConfigurationBuilder WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance.ForAgent(_agent);
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }

    public IAgentConfigurationBuilder WithKnowledge(KnowledgeBuilder knowledge)
    {
        _knowledge = knowledge.ForAgent(_agent).Build();
        return this;
    }

    public IAgentConfigurationBuilder WithKnowledge(Knowledge knowledge)
    {
        _knowledge = knowledge;
        return this;
    }

    public IAgentConfigurationBuilder WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance
            .ForAgent(_agent)
            .DisablePersistence();
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }

    public IAgentConfigurationBuilder WithBehaviour(string name, string instruction)
    {
        _agent.Behaviours ??= [];
        _agent.Behaviours[name] = instruction;
        _agent.CurrentBehaviour = name;
        return this;
    }

    public async Task<IAgentContextExecutor> CreateAsync(bool flow = false, bool interactiveResponse = false)
    {
        ApplyPendingSkills();

        if (_ensureModelDownloaded && !string.IsNullOrWhiteSpace(_agent.Model))
        {
            await AIHub.Model().EnsureDownloadedAsync(_agent.Model);
        }

        await _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache);
        return this;
    }

    public IAgentContextExecutor Create(bool flow = false, bool interactiveResponse = false)
    {
        ApplyPendingSkills();
        _ = _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache).Result;
        return this;
    }

    private void ApplyPendingSkills()
    {
        if (_skillComposer is null || _skillRegistry is null) return;
        if (_pendingSkillNames.Count == 0 && _pendingInlineSkills.Count == 0) return;

        var namedSkills = _pendingSkillNames.Select(name => _skillRegistry.GetSkill(name));
        var allSkills = namedSkills.Concat(_pendingInlineSkills).ToList();

        _skillComposer.Apply(_agent, allSkills, _knowledge);

        var newNames = _pendingSkillNames
            .Concat(_pendingInlineSkills.Select(s => s.Name))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Except(_agent.Skills, StringComparer.OrdinalIgnoreCase);

        _agent.Skills.AddRange(newNames);
    }

    public IAgentConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration)
    {
        _agent.ToolsConfiguration = toolsConfiguration;
        return this;
    }

    internal void LoadExistingKnowledgeIfExists()
    {
        _knowledge ??= new Knowledge(_agent);

        try
        {
            _knowledge.Load();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Knowledge cannot be loaded - new one will be created");
        }
    }

    public async Task<ChatResult> ProcessAsync(Chat chat, bool translate = false)
    {
        if (_knowledge is null)
        {
            LoadExistingKnowledgeIfExists();
        }

        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate);
        var message = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.ModelId,
            Message = message,
            CreatedAt = DateTime.Now
        };
    }

    public async Task<ChatResult> ProcessAsync(
        string message,
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge is null)
        {
            LoadExistingKnowledgeIfExists();
        }

        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(new Message()
        {
            Content = message,
            Role = "User",
            Type = MessageType.NotSet,
            Time = DateTime.Now
        });
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.ModelId,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public async Task<ChatResult> ProcessAsync(Message message,
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge is null)
        {
            LoadExistingKnowledgeIfExists();
        }

        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(message);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.ModelId,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public async Task<ChatResult> ProcessAsync(
        IEnumerable<Message> messages,
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge is null)
        {
            LoadExistingKnowledgeIfExists();
        }

        var chat = await _agentService.GetChatByAgent(_agent.Id);
        var systemMsg = chat.Messages.FirstOrDefault(m => m.Role.Equals(
            ServiceConstants.Roles.System,
            StringComparison.InvariantCultureIgnoreCase));
        chat.Messages.Clear();
        if (systemMsg is not null)
        {
            chat.Messages.Add(systemMsg);
        }

        chat.Messages.AddRange(messages);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.ModelId,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public static async Task<AgentContext> FromExisting(
        IAgentService agentService,
        string agentId,
        ISkillRegistry? skillRegistry = null,
        ISkillComposer? skillComposer = null)
    {
        var existingAgent = await agentService.GetAgentById(agentId);
        if (existingAgent is null)
        {
            throw new AgentNotFoundException(agentId);
        }

        var context = new AgentContext(agentService, existingAgent, skillRegistry, skillComposer);
        context.LoadExistingKnowledgeIfExists();
        return context;
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
        if (agent._knowledge is null)
        {
            agent.LoadExistingKnowledgeIfExists();
        }

        return await agent.ProcessAsync(message, translate);
    }
}
