using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Constants;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class AgentService(
    IAgentRepository agentRepository,
    IChatRepository chatRepository,
    ILogger<AgentService> logger,
    INotificationService notificationService,
    IStepProcessor stepProcessor,
    ICommandDispatcher commandDispatcher,
    ILLMServiceFactory llmServiceFactory,
    MaINSettings maInSettings)
    : IAgentService
{
    public async Task<Chat> Process(Chat chat, string agentId, bool translatePrompt = false)
    {
        var agent = await agentRepository.GetAgentById(agentId);
        if (agent == null) 
            throw new ArgumentException("Agent not found."); //TODO candidate for NotFound domain exception
        if (agent.Context == null) 
            throw new ArgumentException("Agent context not found.");

        await notificationService.DispatchNotification(
            NotificationMessageBuilder.ProcessingStarted(agentId, agent.CurrentBehaviour), "ReceiveAgentUpdate");

        try
        {
            chat = await stepProcessor.ProcessSteps(
                agent.Context,
                agent,
                chat,
                async (status, id, progress, behaviour) =>
                {
                    await notificationService.DispatchNotification(
                        NotificationMessageBuilder.CreateActorProgress(id, status, progress, behaviour), "ReceiveAgentUpdate"); //TODO prepare static lookup for magic string :) 
                },
                async c => await chatRepository.UpdateChat(c.Id, c.ToDocument()),
                logger
            );

            await agentRepository.UpdateAgent(agent.Id, agent);

            await notificationService.DispatchNotification(
                NotificationMessageBuilder.ProcessingComplete(agentId, agent.CurrentBehaviour), "ReceiveAgentUpdate");

            return chat;
        }
        catch (Exception)
        {
            await notificationService.DispatchNotification(
                NotificationMessageBuilder.ProcessingFailed(agentId, agent.CurrentBehaviour), "ReceiveAgentUpdate");
            throw;
        }
    }

    public async Task<Agent> CreateAgent(Agent agent, bool flow = false, bool interactiveResponse = false,
        InferenceParams? inferenceParams = null, MemoryParams? memoryParams = null, bool disableCache = false)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Model = agent.Model,
            Name = agent.Name,
            Visual = agent.Model == ImageGenService.LocalImageModels.FLUX,
            InterferenceParams = inferenceParams ?? new InferenceParams(),
            MemoryParams = memoryParams ?? new MemoryParams(),
            Messages = new List<Message>(),
            Interactive = interactiveResponse,
            Backend = agent.Backend,
            Type = flow ? ChatType.Flow : ChatType.Rag,
        };

        if (disableCache)
        {
            chat.Properties.AddProperty(ServiceConstants.Properties.DisableCacheProperty);
        }

        var startCommand = new StartCommand
        {
            Chat = chat,
            InitialPrompt = agent.Context.Instruction
        };

        await commandDispatcher.DispatchAsync(startCommand);

        agent.Started = true;
        agent.Flow = flow;
        agent.Behaviours ??= new Dictionary<string, string>();
        agent.Behaviours.Add("Default", agent.Context.Instruction!);
        agent.CurrentBehaviour = "Default";

        var agentDocument = agent.ToDocument();
        agentDocument.ChatId = chat.Id;

        await chatRepository.AddChat(chat.ToDocument());
        await agentRepository.AddAgent(agentDocument);

        return agent;
    }

    public async Task<Chat> GetChatByAgent(string agentId)
    {
        var agent = await agentRepository.GetAgentById(agentId);
        if (agent == null)
            throw new Exception("Agent not found."); //TODO good candidate for custom exception
        
        var chat = await chatRepository.GetChatById(agent.ChatId);
        return chat!.ToDomain();
    }

    public async Task<Chat> Restart(string agentId)
    {
        var agent = await agentRepository.GetAgentById(agentId);
        if (agent == null)
            throw new Exception("Agent not found."); //TODO good candidate for custom exception
        
        var chat = (await chatRepository.GetChatById(agent.ChatId))!.ToDomain();
        var llmService = llmServiceFactory.CreateService(agent.Backend ?? maInSettings.BackendType);
        await llmService.CleanSessionCache(chat.Id!);
        AgentStateManager.ClearState(agent, chat);

        await chatRepository.UpdateChat(chat.Id!, chat.ToDocument());
        await agentRepository.UpdateAgent(agent.Id, agent);

        return chat;
    }

    public async Task<List<Agent>> GetAgents() =>
        (await agentRepository.GetAllAgents())
        .Select(x => x.ToDomain())
        .ToList();

    public async Task<Agent?> GetAgentById(string id) =>
        (await agentRepository.GetAgentById(id))?.ToDomain();

    public async Task DeleteAgent(string id)
    {
        var chat = await GetChatByAgent(id);
        var llmService = llmServiceFactory.CreateService(chat.Backend ?? maInSettings.BackendType);
        await llmService.CleanSessionCache(chat.Id);
        await chatRepository.DeleteChat(chat.Id);
        await agentRepository.DeleteAgent(id);
    }

    public Task<bool> AgentExists(string id) =>
        agentRepository.Exists(id);
}