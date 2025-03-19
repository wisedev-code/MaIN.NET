using MaIN.Domain.Entities;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps;

public class FetchDataStepHandler(
    ILLMService llmService, 
    ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "FETCH_DATA";
    public string[] SupportedSteps => ["FETCH_DATA", "FETCH_DATA*"];
    
    public async Task<StepResult> Handle(StepContext context)
    {
        // Skip if already processed
        if (context.StepName == "FETCH_DATA*" && context.Chat.Properties.ContainsKey("FETCH_DATA*"))
        {
            return new StepResult { Chat = context.Chat };
        }

        // Get filter value
        context.Chat.Properties.TryGetValue("data_filter", out var filter);
        var fetchCommand = new FetchCommand
        {
            Chat = context.Chat,
            Filter = filter ?? string.Empty,
            Context = context.Agent.Context!.ToDomain()
        };

        // Use the command dispatcher for all source types
        var response = await commandDispatcher.DispatchAsync(fetchCommand);
        if (response == null)
        {
            throw new InvalidOperationException("Data fetch command failed");
        }

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

        // Mark as processed if needed
        if (context.StepName == "FETCH_DATA*")
        {
            context.Chat.Properties["FETCH_DATA*"] = string.Empty;
        }

        return new StepResult { 
            Chat = context.Chat, 
            RedirectMessage = context.Chat.Messages.Last() 
        };
    }

    private async Task ProcessJsonResponse(Message response, StepContext context)
    {
        context.Chat.Messages?.Add(new Message
        {
            Role = "User",
            Content = "Process this data {....}",
            Properties = response.Properties,
        });

        context.Chat.Properties.TryGetValue("data_filter", out var filterVal);
        var memoryChat = CreateMemoryChat(context, filterVal);
        
        var chunker = new JsonChunker();
        var chunksAsList = chunker.ChunkJson(response.Content).ToList();
        var chunks = chunksAsList
            .Select((chunk, index) => new { Key = $"CHUNK_{index + 1}-{chunksAsList.Count}", Value = chunk })
            .ToDictionary(item => item.Key, item => item.Value);
        
        var result = await llmService.AskMemory(memoryChat, chunks);
        var newMessage = result!.Message;
        newMessage.Properties = new() { { "agent_internal", "true" } };
        context.Chat.Messages?.Add(newMessage.ToDomain());
    }

    //TODO proper way for memory chat!
    private static Chat CreateMemoryChat(StepContext context, string? filterVal)
    {
        return new Chat
        {
            Messages = new List<Message>
            {
                new()
                {
                    Content = context.Agent.Behaviours[context.Agent.CurrentBehaviour].Replace("@filter@", filterVal ?? string.Empty),
                    Role = "User"
                }
            },
            Model = context.Chat.Model,
            Properties = context.Chat.Properties,
            Name = "Memory Chat",
            Id = Guid.NewGuid().ToString()
        };
    }
}