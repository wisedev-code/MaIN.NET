using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Transform;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Models;
using MaIN.Models.Rag;
using MaIN.Services.Mappers;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using MongoDB.Driver.Core.Events;

namespace MaIN.Services.Services;

public class AgentService(
    IAgentRepository agentRepository,
    IChatRepository chatRepository, 
    INotificationService notificationService) : IAgentService
{
    
    public async Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false)
    {
        Func<string, string, Task> dispatchNotification = async (status, agentid) =>
        {
            var messageFinal = new Dictionary<string, string>
            {
                { "AgentId", agentid},
                { "IsProcessing", status }
            };
            await notificationService.DispatchNotification(messageFinal);
        };
        
        Func<Chat, Task> updateChat = async (chatObj) =>
        {
            await chatRepository.UpdateChat(chatObj.Id, chatObj.ToDocument());
        };
        
        // Fetch the agent details from the repository
        var agent = await agentRepository.GetAgentById(agentId);
        await dispatchNotification("true", agentId);

        // Ensure the agent and its context are valid
        if (agent == null)
        {
            throw new ArgumentException("Agent not found.");
        }

        var context = agent.Context;
        if (context == null)
        {
            throw new ArgumentException("Agent context not found.");
        }

        chat = await ProcessSteps(context, agent, chat, dispatchNotification, updateChat);

        await agentRepository.UpdateAgent(agent.Id, agent);
        await dispatchNotification("false", agentId);

        // Return the processed chat
        return chat;
    }

    private static async Task<Chat?> ProcessSteps(AgentContextDocument context, AgentDocument agent, Chat? chat,
        Func<string, string, Task> dispatchNotification, Func<Chat, Task> updateChat)
    {
        // Process each step in the defined order
        foreach (var step in context.Steps)
        {
            // provide arguments
            var stepParts = step.Split('+');
            var stepName = stepParts[0];
            var shouldReplaceLastMessage = step.Contains("REPLACE");

            // Create the appropriate command based on the step name
            switch (stepName)
            {
                case "REDIRECT":
                    var redirectCommand = new RedirectCommand
                    {
                        Message = chat?.Messages?.Last()!,
                        RelatedAgentId = stepParts[1],
                        SaveAs = Enum.Parse<OutputTypeOfRedirect>(stepParts[2]),
                        Filter = chat?.Properties.GetValueOrDefault("data_filter")
                    };

                    await dispatchNotification("false", agent.Id);
                    
                    var message = await Actions.CallAsync("REDIRECT", redirectCommand) as Message;
                    if (redirectCommand.SaveAs == OutputTypeOfRedirect.AS_Filter)
                    {
                        chat?.Properties.TryAdd("data_filter", message!.Content);
                    }
                    else
                    {
                        if (shouldReplaceLastMessage)
                        {
                            chat?.Messages?.RemoveAt(chat.Messages.Count - 1);
                        }

                        message!.Time = DateTime.Now;
                        chat?.Messages?.Add(message);
                    }

                    break;

                case "FETCH_DATA":
                    var filterExist =
                        chat!.Properties.TryGetValue("data_filter",
                            out var filter); //TODO define a way to create multiple filters
                    var fetchCommand = new FetchCommand
                    {
                        Chat = chat,
                        Filter = filterExist ? filter : string.Empty,
                        Context = context.ToDomain()
                    };
                    var fetchCommandResponse =
                        (await Actions.CallAsync("FETCH_DATA", fetchCommand) as Message)!;
                    chat.Messages?.Add(fetchCommandResponse);
                    break;

                case "FETCH_DATA*":
                    if (chat!.Properties.ContainsKey("FETCH_DATA*"))
                    {
                        break;
                    }

                    var filterExists =
                        chat!.Properties.TryGetValue("data_filter",
                            out var filterData); //TODO define a way to create multiple filters
                    var fetchCommandOnce = new FetchCommand
                    {
                        Context = context.ToDomain(),
                        Chat = chat,
                        Filter = filterExists ? filterData : string.Empty
                    };
                    var response = (await Actions.CallAsync("FETCH_DATA", fetchCommandOnce) as Message)!;
                    chat.Messages?.Add(response);

                    chat.Properties.Add("FETCH_DATA*", string.Empty);
                    break;

                case "ANSWER":
                    var answerCommand = new AnswerCommand
                    {
                        Chat = chat
                    };
                    var answerResponse = (await Actions.CallAsync("ANSWER", answerCommand) as Message)!;

                    if (shouldReplaceLastMessage)
                    {
                        chat?.Messages?.RemoveAt(chat.Messages.Count - 1);
                    }

                    var filterVal = GetFilter(answerResponse.Content);
                    if (!string.IsNullOrEmpty(filterVal))
                    {
                        chat?.Properties.TryAdd("data_filter", filterVal);
                    }
                    answerResponse.Time = DateTime.Now;
                    chat?.Messages?.Add(answerResponse);
                    break;
                
                case "BECOME":
                    agent.Context.Instruction = agent.Behaviours.GetValueOrDefault(stepParts[1]) ?? agent.Context.Instruction;
                    agent.CurrentBehaviour = stepParts[1];
                    chat!.Messages![0].Content = agent.Context.Instruction;
                    break;
                
                case "CLEANUP":
                    chat!.Messages = chat.Messages!.Take(1).ToList();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown step: {stepName}");
            }
            
            await updateChat(chat!);
        }

        return chat;
    }

    private static string? GetFilter(string? content)
    {
        var pattern = @"filter::\{(.*?)\}";
        var match = Regex.Match(content!, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<Agent> CreateAgent(Agent agent)
    {
        var chat = new Chat()
        {
            Id = Guid.NewGuid().ToString(),
            Model = agent.Model,
            Name = agent.Name,
            Stream = false,
            Messages = new List<Message>(),
            Type = ChatType.Rag,
        };

        var startCommand = new StartCommand()
        {
            Chat = chat,
            InitialPrompt = agent.Context.Instruction,
        };

        var result = await Actions.CallAsync("START", startCommand) as Message;
        result!.Role = "system";
        agent.Started = true;
        agent.Behaviours.Add("Default", agent.Context.Instruction);
        agent.CurrentBehaviour = "Default";
        var agentDocument = agent.ToDocument();
        agentDocument!.ChatId = chat.Id;
        await chatRepository.AddChat(chat!.ToDocument());
        await agentRepository.AddAgent(agentDocument);
        return agent;
    }

    public async Task<Chat> GetChatByAgent(string agentId)
    {
        var agent = await agentRepository.GetAgentById(agentId);
        var chat = await chatRepository.GetChatById(agent.ChatId);
        return chat.ToDomain();
    }

    public async Task<Chat> Restart(string agentId)
    {
        var agent = await agentRepository.GetAgentById(agentId);
        var chat = await chatRepository.GetChatById(agent?.ChatId!);
        chat.Messages = chat.Messages.Take(1).ToList(); //Takes only system message and initial prompt
        await chatRepository.UpdateChat(chat.Id, chat);

        return chat.ToDomain();
    }

    public async Task<List<Agent>> GetAgents()
    {
        var result = await agentRepository.GetAllAgents();
        return result
            .Select(x => x.ToDomain())
            .ToList()!;
    }

    public async Task<Agent?> GetAgentById(string id)
    {
        var result = await agentRepository.GetAgentById(id);
        return result?.ToDomain();
    }

    public async Task DeleteAgent(string id)
    {
        var chat = await GetChatByAgent(id);
        await chatRepository.DeleteChat(chat.Id);
        await agentRepository.DeleteAgent(id);
    }

    public Task<bool> AgentExists(string id) =>
        agentRepository.Exists(id);
}