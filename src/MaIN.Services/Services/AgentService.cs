using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using MaIN.Services.Steps;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<AgentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IStepProcessor _stepProcessor;
    private readonly ILLMService _llmService;

    public AgentService(
        IAgentRepository agentRepository,
        IChatRepository chatRepository,
        ILogger<AgentService> logger,
        INotificationService notificationService,
        IStepProcessor stepProcessor, 
        ILLMService llmService)
    {
        _agentRepository = agentRepository;
        _chatRepository = chatRepository;
        _logger = logger;
        _notificationService = notificationService;
        _stepProcessor = stepProcessor;
        _llmService = llmService;
    }

    public async Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false)
    {
        var agent = await _agentRepository.GetAgentById(agentId);
        if (agent == null) throw new ArgumentException("Agent not found.");
        if (agent.Context == null) throw new ArgumentException("Agent context not found.");

        await _notificationService.DispatchNotification(
            NotificationMessageBuilder.ProcessingStarted(agentId, agent.CurrentBehaviour!), "ReceiveAgentUpdate");

        try
        {
            chat = await _stepProcessor.ProcessSteps(
                agent.Context,
                agent,
                chat,
                async (status, id, progress, behaviour) =>
                {
                    await _notificationService.DispatchNotification(
                        NotificationMessageBuilder.CreateActorProgress(id, status, progress, behaviour), "ReceiveAgentUpdate");
                },
                async c => await _chatRepository.UpdateChat(c.Id, c.ToDocument()),
                _logger
            );

            await _agentRepository.UpdateAgent(agent.Id, agent);

            await _notificationService.DispatchNotification(
                NotificationMessageBuilder.ProcessingComplete(agentId, agent.CurrentBehaviour!), "ReceiveAgentUpdate");

            return chat;
        }
        catch (Exception)
        {
            await _notificationService.DispatchNotification(
                NotificationMessageBuilder.ProcessingFailed(agentId, agent.CurrentBehaviour!), "ReceiveAgentUpdate");
            throw;
        }
    }

    public async Task<Agent> CreateAgent(Agent agent, bool flow = false, bool interactiveResponse = false,
        InferenceParams? inferenceParams = null)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Model = agent.Model,
            Name = agent.Name,
            Visual = agent.Model == ImageGenService.Models.FLUX,
            InterferenceParams = inferenceParams ?? new InferenceParams(),
            Messages = new List<Message>(),
            Interactive = interactiveResponse,
            Type = flow ? ChatType.Flow : ChatType.Rag,
        };

        var startCommand = new StartCommand
        {
            Chat = chat,
            InitialPrompt = agent.Context.Instruction
        };

        await Actions.CallAsync("START", startCommand);

        agent.Started = true;
        agent.Flow = flow;
        agent.Behaviours ??= new Dictionary<string, string>();
        agent.Behaviours.Add("Default", agent.Context.Instruction);
        agent.CurrentBehaviour = "Default";

        var agentDocument = agent.ToDocument();
        agentDocument!.ChatId = chat.Id;

        await _chatRepository.AddChat(chat.ToDocument());
        await _agentRepository.AddAgent(agentDocument);

        return agent;
    }

    public async Task<Chat?> GetChatByAgent(string agentId)
    {
        var agent = await _agentRepository.GetAgentById(agentId);
        var chat = await _chatRepository.GetChatById(agent.ChatId);
        return chat.ToDomain();
    }

    public async Task<Chat?> Restart(string agentId)
    {
        var agent = await _agentRepository.GetAgentById(agentId);
        var chat = (await _chatRepository.GetChatById(agent?.ChatId!)).ToDomain();
        await _llmService.CleanSessionCache(chat.Id);
        AgentStateManager.ClearState(agent, chat);

        await _chatRepository.UpdateChat(chat.Id, chat.ToDocument());
        await _agentRepository.UpdateAgent(agent!.Id, agent);

        return chat;
    }

    public async Task<List<Agent>> GetAgents() =>
        (await _agentRepository.GetAllAgents())
        .Select(x => x.ToDomain())
        .ToList()!;

    public async Task<Agent?> GetAgentById(string id) =>
        (await _agentRepository.GetAgentById(id))?.ToDomain();

    public async Task DeleteAgent(string id)
    {
        var chat = await GetChatByAgent(id);
        await _llmService.CleanSessionCache(chat.Id);
        await _chatRepository.DeleteChat(chat.Id);
        await _agentRepository.DeleteAgent(id);
    }

    public Task<bool> AgentExists(string id) =>
        _agentRepository.Exists(id);
}