using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Steps;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps;

public class FetchDataStepHandler(ILLMService llmService) : IStepHandler
{
    public string StepName => "FETCH_DATA";

    public string[] SupportedSteps => ["FETCH_DATA", "FETCH_DATA*"];
    private static string _temporaryChatId = Guid.NewGuid().ToString();

    public async Task<StepResult> Handle(StepContext context)
    {
        //TODO refactor it for proper usage
        if (context.StepName == "FETCH_DATA*" && context.Chat!.Properties.ContainsKey("FETCH_DATA*"))
        {
            return new StepResult { Chat = context.Chat };
        }

        var filterExists = context.Chat!.Properties.TryGetValue("data_filter", out var filter);
        var fetchCommand = new FetchCommand
        {
            Chat = context.Chat,
            Filter = filterExists ? filter : string.Empty,
            Context = context.Agent.Context!.ToDomain()
        };

        if (fetchCommand.Context.Source!.Type == AgentSourceType.File)
        {
            var data = JsonSerializer.Deserialize<AgentFileSourceDetails>(fetchCommand.Context.Source.Details?.ToString()!);
            var memoryChat = GetMemoryChat(context, filter);
            var result = await llmService.AskMemory(memoryChat, fileData: new Dictionary<string, string>() { { data!.Name, data.Path } });
            var newMessage = result!.Message;
            newMessage!.Properties = new() { { "agent_internal", "true" } };
            context.Chat.Messages?.Add(newMessage.ToDomain());
            return new StepResult { Chat = context.Chat, RedirectMessage = context.Chat!.Messages!.Last() };
        }
        
        if (fetchCommand.Context.Source!.Type == AgentSourceType.Web)
        {
            var data = JsonSerializer.Deserialize<AgentWebSourceDetails>(fetchCommand.Context.Source.Details?.ToString()!);
            var memoryChat = GetMemoryChat(context, filter);
            var result = await llmService.AskMemory(memoryChat, webUrls: [data!.Url]);
            var newMessage = result!.Message;
            newMessage.Properties = new() { { "agent_internal", "true" } };
            context.Chat.Messages.Add(newMessage.ToDomain());
            return new StepResult { Chat = context.Chat, RedirectMessage = context.Chat!.Messages!.Last() };
        }

        var response = (await Actions.CallAsync("FETCH_DATA", fetchCommand) as Message)!;

        if (response.Properties.ContainsValue("JSON"))
        {
            await ProcessJsonResponse(response, context);
        }
        else
        {
            context.Chat.Messages.Add(new Message
            {
                Role = "System",
                Content = $"Remember this data: {response.Content}",
                Properties = response.Properties
            });
        }

        if (context.StepName == "FETCH_DATA*")
        {
            context.Chat.Properties.Add("FETCH_DATA*", string.Empty);
        }

        return new StepResult { Chat = context.Chat, RedirectMessage = context.Chat!.Messages!.Last() };
    }

    private async Task ProcessJsonResponse(Message response, StepContext context)
    {
        context.Chat!.Messages?.Add(new Message
        {
            Role = "User",
            Content = "Process this data {....}",
            Properties = response.Properties,
        });

        context.Chat!.Properties.TryGetValue("data_filter", out var filterVal);
        var memoryChat = GetMemoryChat(context, filterVal);
        
        var chunker = new JsonChunker();
        var chunksAsList = chunker.ChunkJson(response.Content).ToList();
        var chunks = chunksAsList
            .Select((chunk, index) => new { Key = $"CHUNK_{index + 1}-{chunksAsList.Count}", Value = chunk })
            .ToDictionary(item => item.Key, item => item.Value);
        var result = await llmService.AskMemory(memoryChat, chunks);
        var newMessage = result!.Message;
        newMessage!.Properties = new() { { "agent_internal", "true" } };
        context.Chat.Messages?.Add(newMessage.ToDomain());
    }

    private static Chat GetMemoryChat(StepContext context, string? filterVal)
    {
        var memoryChat = new Chat()
        {
            Messages = new List<Message>
            {
                new()
                {
                    Content = context.Agent.Behaviours[context.Agent.CurrentBehaviour].Replace("@filter@", filterVal),
                    Role = "User"
                }
            },
            Model = context.Chat.Model,
            Properties = context.Chat.Properties,
            Id = Guid.NewGuid().ToString()
        };
        return memoryChat;
    }

    private static async Task ProcessChunk(string chunk, int index, int total, StepContext context,
        Dictionary<string, string> responseProperties)
    {
        await context.NotifyProgress("true", context.Agent.Id, $"{index + 1}/{total}",
            context.Agent.CurrentBehaviour);

        var addition = total == index + 1 ? "Process it" : "Process it, and output processed data";
        var message = $"{chunk} - {addition}";
        context.Chat!.Messages?.Add(new Message
        {
            Role = "User",
            Content = message,
            Properties = responseProperties,
        });

        var temporaryChat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Model = context.Chat.Model,
            Messages = new List<Message>
            {
                context.Chat.Messages!.First(),
                new() { Role = "User", Content = message }
            }
        };
        
        var newMessage = await Actions.CallAsync("ANSWER", new AnswerCommand
        {
            Chat = temporaryChat,
            LastChunk = index == total - 1,
            TemporaryChat = true
        }) as Message;

        newMessage!.Properties = new() { { "agent_internal", "true" } };
        context.Chat.Messages?.Add(newMessage);
    }
}