using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Steps;

public class FetchDataStepHandler(
    ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "FETCH_DATA";
    public string[] SupportedSteps => ["FETCH_DATA", "FETCH_DATA*"];
    
    public async Task<StepResult> Handle(StepContext context)
    {
        var respondAsSystem = context.Arguments.Contains("AS_SYSTEM");
        if (context.StepName == "FETCH_DATA*" && context.Chat.Properties.ContainsKey("FETCH_DATA*"))
        {
            return new StepResult { Chat = context.Chat };
        }

        context.Chat.Properties.TryGetValue("data_filter", out var filter);
        var fetchCommand = new FetchCommand
        {
            Chat = context.Chat,
            Filter = filter ?? string.Empty,
            Context = context.Agent.Context!.ToDomain(),
            ResponseType = respondAsSystem ? FetchResponseType.AS_System : FetchResponseType.AS_Answer,
            MemoryChat = CreateMemoryChat(context, filter)
        };

        var response = await commandDispatcher.DispatchAsync(fetchCommand);
        if (response == null)
        {
            throw new CommandFailedException(fetchCommand.CommandName);
        }

        if (context.StepName == "FETCH_DATA*")
        {
            context.Chat.Properties["FETCH_DATA*"] = string.Empty;
        }
        
        context.Chat.Messages.Add(response);
        
        return new StepResult { 
            Chat = context.Chat, 
            RedirectMessage = context.Chat.Messages.Last() 
        };
    }
    
    private static Chat CreateMemoryChat(StepContext context, string? filterVal)
    {
        return new Chat
        {
            Messages = new List<Message>
            {
                new()
                {
                    Content = context.Agent.Behaviours[context.Agent.CurrentBehaviour].Replace("@filter@", filterVal ?? string.Empty),
                    Type = context.Chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM,
                    Role = "User"
                }
            },
            Model = context.Chat.Model,
            Properties = context.Chat.Properties,
            MemoryParams = context.Chat.MemoryParams,
            InterferenceParams = context.Chat.InterferenceParams,
            Backend = context.Chat.Backend,
            Name = "Memory Chat",
            Id = Guid.NewGuid().ToString()
        };
    }
}