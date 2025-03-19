using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Steps;

public class RedirectStepHandler(ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "REDIRECT";

    public async Task<StepResult> Handle(StepContext context)
    {
        var shouldReplaceLastMessage = context.Arguments.Contains("REPLACE");
        var shouldWorkAsUserMessage = context.Arguments.Contains("USER");
        var useMemory = context.Arguments.Contains("MEMORY");
        var redirectCommand = new RedirectCommand
        {
            Message = context.RedirectMessage,
            RelatedAgentId = context.Arguments[0],
            SaveAs = Enum.Parse<OutputTypeOfRedirect>(context.Arguments[1]),
            Filter = context.Chat.Properties.GetValueOrDefault("data_filter")
        };

        await context.NotifyProgress("false", context.Agent.Id, null, context.Agent.CurrentBehaviour);

        var message = await commandDispatcher.DispatchAsync(redirectCommand);

        if (useMemory)
        {
            context.Chat?.Memory.Add(message!.Content);
        }
        
        if (redirectCommand.SaveAs == OutputTypeOfRedirect.AS_Filter)
        {
            context.Chat?.Properties.TryAdd("data_filter", message!.Content);
        }
        else
        {
            if (shouldReplaceLastMessage)
            {
                var lastMsg = (context.Chat!.Messages)[^1];
                message!.Properties = lastMsg.Properties ?? [];
                message.Role = lastMsg.Role;
                context.Chat?.Messages?.RemoveAt(context.Chat.Messages.Count - 1);
            }

            if (shouldWorkAsUserMessage)
            {
                message!.Role = "User";
            }
            message!.Time = DateTime.Now;
            context.Chat?.Messages?.Add(message);
        }

        return new StepResult { Chat = context.Chat!, RedirectMessage = message };
    }
}
