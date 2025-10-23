using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Steps;

public class BecomeStepHandler : IStepHandler
{
    public string StepName => "BECOME";
    public string[] SupportedSteps => ["BECOME", "BECOME*"];


    public async Task<StepResult> Handle(StepContext context)
    {
        if (context.StepName == "BECOME*" && context.Chat.Properties.ContainsKey("BECOME*"))
        {
            return new StepResult { Chat = context.Chat };
        }
        
        var newBehaviour = context.Arguments[0];
        var messageFilter = context.Agent.Behaviours.GetValueOrDefault(newBehaviour) ?? 
                            context.Agent.Context!.Instruction;

        if (context.Chat.Properties.TryGetValue("data_filter", out var filterQuery))
        {
            messageFilter = context.Agent.Behaviours.GetValueOrDefault(newBehaviour)!
                .Replace("@filter@", filterQuery);
            context.TagsToReplaceWithFilter.Add(filterQuery);
        }

        context.Agent.CurrentBehaviour = newBehaviour;
        context.Chat.Messages[0].Content = messageFilter ?? context.Agent.Context!.Instruction!;
        
        await context.NotifyProgress("true", context.Agent.Id, null, context.Agent.CurrentBehaviour, StepName);

        context.Chat.Messages.Add(new()
        {
            Role = "System",
            Content = $"Now - {messageFilter}",
            Properties = new() {{"agent_internal", "true"}},
            Type = context.Chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM
        });
        
        if (context.StepName == "BECOME*")
        {
            context.Chat.Properties.Add("BECOME*", string.Empty);
        }
        
        return new() { Chat = context.Chat };
    }
}