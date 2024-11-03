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
        if (context.StepName == "BECOME*" && context.Chat!.Properties.ContainsKey("BECOME*"))
        {
            return new StepResult { Chat = context.Chat };
        }
        
        var newBehaviour = context.Arguments[0];
        var messageFilter = context.Agent.Behaviours.GetValueOrDefault(newBehaviour) ?? 
                            context.Agent.Context.Instruction;

        if (context.Chat!.Properties.TryGetValue("data_filter", out var filterQuery))
        {
            messageFilter = context.Agent.Behaviours.GetValueOrDefault(newBehaviour)!
                .Replace("@filter@", filterQuery);
            context.TagsToReplaceWithFilter.Add(filterQuery);
        }

        context.Agent.CurrentBehaviour = newBehaviour;
        context.Chat.Messages![0].Content = messageFilter;
        
        await context.NotifyProgress("true", context.Agent.Id, null, context.Agent.CurrentBehaviour);

        context.Chat.Messages?.Add(new()
        {
            Role = "user",
            Content = $"Now - {messageFilter}",
            Properties = new() {{"agent_internal", "true"}}
        });
        
        if (context.StepName == "BECOME*")
        {
            context.Chat.Properties.Add("BECOME*", string.Empty);
        }
        
        return new() { Chat = context.Chat };
    }
}