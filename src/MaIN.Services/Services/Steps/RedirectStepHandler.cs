using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Steps;

namespace MaIN.Services.Services.Steps;

public class RedirectStepHandler : IStepHandler
{
    public string StepName => "REDIRECT";

    public async Task<StepResult> Handle(StepContext context)
    {
        var shouldReplaceLastMessage = context.Arguments.Contains("REPLACE");
        var redirectCommand = new RedirectCommand
        {
            Message = context.RedirectMessage,
            RelatedAgentId = context.Arguments[0],
            SaveAs = Enum.Parse<OutputTypeOfRedirect>(context.Arguments[1]),
            Filter = context.Chat?.Properties.GetValueOrDefault("data_filter")
        };

        await context.NotifyProgress("false", context.Agent.Id, null, context.Agent.CurrentBehaviour);

        var message = await Actions.CallAsync("REDIRECT", redirectCommand) as Message;
        
        if (redirectCommand.SaveAs == OutputTypeOfRedirect.AS_Filter)
        {
            context.Chat?.Properties.TryAdd("data_filter", message!.Content);
        }
        else
        {
            if (shouldReplaceLastMessage)
            {
                var msgprops = context.Chat?.Messages![^1].Properties;
                message!.Properties = msgprops ?? [];
                context.Chat?.Messages?.RemoveAt(context.Chat.Messages.Count - 1);
            }

            message!.Time = DateTime.Now;
            context.Chat?.Messages?.Add(message);
        }

        return new StepResult { Chat = context.Chat, RedirectMessage = message };
    }
}
