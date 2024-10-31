using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Steps;

public class BecomeStepHandler : IStepHandler
{
    public string StepName => "BECOME";

    public async Task<StepResult> Handle(StepContext context)
    {
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

        context.Chat.Messages?.Add(new Message
        {
            Role = "user",
            Content = $"Now - {messageFilter}"
        });

        return new StepResult { Chat = context.Chat };
    }
}