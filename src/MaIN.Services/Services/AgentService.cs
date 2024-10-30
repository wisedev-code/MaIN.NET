using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Transform;
using Amazon.Runtime.Internal.Util;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class AgentService(
    IAgentRepository agentRepository,
    IChatRepository chatRepository,
    ILogger<AgentService> logger,
    INotificationService notificationService) : IAgentService
{
    public async Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false)
    {
        Func<string, string, string?, string, Task> dispatchNotification = async (status, agentid, progress, behaviour ) =>
        {
            var messageFinal = new Dictionary<string, string>
            {
                { "AgentId", agentid },
                { "IsProcessing", status },
                { "Progress", progress },
                { "Behaviour", behaviour }
            };
            await notificationService.DispatchNotification(messageFinal);
        };

        Func<Chat, Task> updateChat = async (chatObj) =>
        {
            await chatRepository.UpdateChat(chatObj.Id, chatObj.ToDocument());
        };

        // Fetch the agent details from the repository
        var agent = await agentRepository.GetAgentById(agentId);
        await dispatchNotification("true", agentId, null, agent?.CurrentBehaviour!);

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

        chat = await ProcessSteps(
            context,
            agent,
            chat,
            dispatchNotification,
            updateChat,
            logger);

        await agentRepository.UpdateAgent(agent.Id, agent);
        await dispatchNotification("false", agentId, "DONE", agent.CurrentBehaviour);

        // Return the processed chat
        return chat;
    }

    private static async Task<Chat?> ProcessSteps(
        AgentContextDocument context,
        AgentDocument agent,
        Chat? chat,
        Func<string, string, string?, string, Task> dispatchNotification,
        Func<Chat, Task> updateChat,
        ILogger<AgentService> logger)
    {
        Message redirectMessage = chat?.Messages?.Last()!;
        var tagsToReplaceWithFilter = new List<string>();
        // Process each step in the defined order
        foreach (var step in context.Steps)
        {
            logger.LogInformation("Processing step: {Step} on agent {agent}", step, agent.Name);
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
                        Message = redirectMessage,
                        RelatedAgentId = stepParts[1],
                        SaveAs = Enum.Parse<OutputTypeOfRedirect>(stepParts[2]),
                        Filter = chat?.Properties.GetValueOrDefault("data_filter")
                    };

                    await dispatchNotification("false", agent.Id, null, agent.CurrentBehaviour);

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

                    if (fetchCommandResponse.Properties.ContainsValue("JSON"))
                    {
                        var data = fetchCommandResponse.Content;
                        var splitter = new JsonChunker(maxTokens: 2000);
                        var chunks = splitter.ChunkJson(data).ToList();
                        if (fetchCommandResponse.Properties.ContainsKey("chunk_limit"))
                        {
                            chunks = chunks.Take(int.Parse(fetchCommandResponse.Properties.GetValueOrDefault("chunk_limit")!)).ToList();
                        }
                        int index = 0;
                        foreach (var chunk in chunks)
                        {
                            await dispatchNotification("true", agent.Id, $"{index + 1}/{chunks.Count}",
                                agent.CurrentBehaviour);

                            logger.LogDebug("Processing chunk: {chunk} out of {chunks}", index, chunks.Count);
                            var addition = chunks.Count == index + 1
                                ? "Remember it"
                                : "Remember it and wait for the next message";
                            chat.Messages?
                                .Add(new Message
                                {
                                    Role = "user",
                                    Content = $"[Chunk {index + 1}/{chunks.Count}] {chunk} - {addition}"
                                });

                            var newMessage = await Actions.CallAsync("ANSWER", new AnswerCommand()
                            {
                                Chat = chat,
                            }) as Message;

                            chat.Messages?.Add(newMessage!);
                            index++;
                        }
                    }
                    else
                    {
                        chat.Messages?
                            .Add(new Message
                            {
                                Role = "user",
                                Content = $"Remember this data: {fetchCommandResponse.Content}"
                            });
                    }

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
                    if (response.Properties.ContainsValue("JSON"))
                    {
                        var data = response.Content;
                        var splitter = new JsonChunker(maxTokens: 2000);
                        var chunks = splitter.ChunkJson(data).ToList();
                        if (response.Properties.ContainsKey("chunk_limit"))
                        {
                            chunks = chunks.Take(int.Parse(response.Properties.GetValueOrDefault("chunk_limit")!)).ToList();
                        }
                        int index = 0;
                        foreach (var chunk in chunks)
                        {
                            await dispatchNotification("true", agent.Id, $"{index + 1}/{chunks.Count}",
                                agent.CurrentBehaviour);

                            logger.LogDebug("Processing chunk: {chunk} out of {chunks}", index, chunks.Count);
                            var addition = chunks.Count == index + 1
                                ? "Remember it"
                                : "Remember it and wait for the next message";
                            chat.Messages?
                                .Add(new Message
                                {
                                    Role = "user",
                                    Content = $"[Chunk {index + 1}/{chunks.Count}] {chunk} - {addition}"
                                });

                            var newMessage = await Actions.CallAsync("ANSWER", new AnswerCommand()
                            {
                                Chat = chat,
                            }) as Message;

                            chat.Messages?.Add(newMessage!);
                            index++;
                        }
                    }
                    else
                    {
                        chat.Messages?
                            .Add(new Message
                            {
                                Role = "user",
                                Content = $"Remember this data: {response.Content}"
                            });
                    }
                    chat.Properties.Add("FETCH_DATA*", string.Empty);
                    break;

                case "ANSWER":
                    await dispatchNotification("true", agent.Id, null, agent.CurrentBehaviour);

                    var answerCommand = new AnswerCommand
                    {
                        Chat = chat
                    };
                    var answerResponse = (await Actions.CallAsync("ANSWER", answerCommand) as Message)!;

                    var filterVal = GetFilter(answerResponse.Content);
                    if (!string.IsNullOrEmpty(filterVal))
                    {
                        chat?.Properties.TryAdd("data_filter", filterVal);
                    }
                    
                    answerResponse.Time = DateTime.Now;
                    redirectMessage = answerResponse;
                    chat?.Messages?.Add(answerResponse);
                    break;

                case "BECOME":
                    var messageFilter = agent.Behaviours.GetValueOrDefault(stepParts[1]) ?? agent.Context.Instruction;
                    if (chat!.Properties.TryGetValue("data_filter", out var filterQuery))
                    {
                        messageFilter = agent.Behaviours.GetValueOrDefault(stepParts[1])!.Replace("@filter@", filterQuery);
                        tagsToReplaceWithFilter.Add(filterQuery);
                    }

                    agent.CurrentBehaviour = stepParts[1];
                    chat.Messages![0].Content = messageFilter;
                    await dispatchNotification("true", agent.Id, null, agent.CurrentBehaviour);

                    chat.Messages?.Add(new Message()
                    {
                        Role = "user",
                        Content = $"Now - {messageFilter}"
                    });
                    break;

                case "CLEANUP":
                    ClearAgentState(agent, chat);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown step: {stepName}");
            }

            await updateChat(chat!);
        }

        //rollback behaviour to default
        foreach (var key in agent.Behaviours.Keys.ToList())
        {
            agent.Behaviours[key] = tagsToReplaceWithFilter.Aggregate(
                agent.Behaviours[key], 
                (current, tag) => current.Replace(tag, "@filter@"));
        }
        
        return chat;
    }

    private static string? GetFilter(string? content)
    {
        var pattern = @"filter:?:?\{(.*?)\}";        
        var match = Regex.Match(content!, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<Agent> CreateAgent(Agent agent, bool flow = false)
    {
        var chat = new Chat()
        {
            Id = Guid.NewGuid().ToString(),
            Model = agent.Model,
            Name = agent.Name,
            Visual = agent.Model == ImageGenService.Models.FLUX,
            Stream = false,
            Messages = new List<Message>(),
            Type = flow ? ChatType.Flow : ChatType.Rag,
        };

        var startCommand = new StartCommand()
        {
            Chat = chat,
            InitialPrompt = agent.Context.Instruction,
        };

        await Actions.CallAsync("START", startCommand);
        agent.Started = true;
        agent.Flow = flow;
        agent.Behaviours ??= new Dictionary<string, string>();
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
        var chat = (await chatRepository.GetChatById(agent?.ChatId!)).ToDomain();
        ClearAgentState(agent, chat);

        await chatRepository.UpdateChat(chat.Id, chat.ToDocument());
        await agentRepository.UpdateAgent(agent!.Id, agent);

        return chat;
    }

    private static void ClearAgentState(AgentDocument? agent, Chat chat)
    {
        agent!.CurrentBehaviour = "Default";
        chat.Properties.Clear();
        if (chat.Model == ImageGenService.Models.FLUX)
        {
            chat.Messages = [];
        }
        else
        {
            //Takes only system message and initial prompt
            chat.Messages![0].Content = agent.Context.Instruction;
            chat.Messages = chat.Messages.Take(1).ToList();
        }
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