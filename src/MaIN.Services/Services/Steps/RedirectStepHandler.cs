using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Steps;

public class RedirectStepHandler(ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "REDIRECT";

    public async Task<StepResult> Handle(StepContext context)
    {
        if (context.Arguments == null || context.Arguments.Length < 2)
        {
            throw new ArgumentException("Redirect step requires at least two arguments: RelatedAgentId and SaveAs type");
        }

        var options = ParseOptions(context.Arguments);
        var redirectCommand = CreateRedirectCommand(context, options);

        await context.NotifyProgress("false", context.Agent.Id, null, context.Agent.CurrentBehaviour, StepName);

        var message = await commandDispatcher.DispatchAsync(redirectCommand);
        if (message == null)
        {
            throw new InvalidOperationException("Redirect command failed to produce a message");
        }

        return ProcessResult(context, message, options, redirectCommand.SaveAs);
    }

    private record RedirectOptions(
        bool ShouldReplaceLastMessage,
        bool ShouldWorkAsUserMessage,
        bool UseMemory
    );

    private RedirectOptions ParseOptions(string[] arguments)
    {
        return new RedirectOptions(
            ShouldReplaceLastMessage: arguments.Contains("REPLACE"),
            ShouldWorkAsUserMessage: arguments.Contains("USER"),
            UseMemory: arguments.Contains("MEMORY")
        );
    }

    private RedirectCommand CreateRedirectCommand(StepContext context, RedirectOptions options)
    {
        return new RedirectCommand
        {
            Chat = context.Chat,
            Message = context.RedirectMessage,
            RelatedAgentId = context.Arguments[0],
            SaveAs = Enum.Parse<OutputTypeOfRedirect>(context.Arguments[1]),
            Filter = context.Chat.Properties.GetValueOrDefault("data_filter")
        };
    }

    private StepResult ProcessResult(
        StepContext context, 
        Message message, 
        RedirectOptions options,
        OutputTypeOfRedirect saveAs)
    {
        if (options.UseMemory && context.Chat?.Memory != null)
        {
            context.Chat.Memory.Add(message.Content);
        }
        
        if (saveAs == OutputTypeOfRedirect.AS_Filter)
        {
            context.Chat?.Properties.TryAdd("data_filter", message.Content);
        }
        else
        {
            AddMessageToChat(context, message, options);
        }

        return new StepResult { Chat = context.Chat!, RedirectMessage = message };
    }

    private void AddMessageToChat(StepContext context, Message message, RedirectOptions options)
    {
        if (context.Chat?.Messages == null)
        {
            return;
        }

        if (options.ShouldReplaceLastMessage && context.Chat.Messages.Count > 0)
        {
            ReplaceLastMessage(context, message);
        }
        else
        {
            AddNewMessage(context, message, options.ShouldWorkAsUserMessage);
        }
    }

    private void ReplaceLastMessage(StepContext context, Message message)
    {
        var lastMsg = context.Chat!.Messages[^1];
        message.Properties = lastMsg.Properties ?? [];
        message.Role = lastMsg.Role;
        context.Chat.Messages.RemoveAt(context.Chat.Messages.Count - 1);
        context.Chat.Messages.Add(message);
    }

    private void AddNewMessage(StepContext context, Message message, bool asUserMessage)
    {
        if (asUserMessage)
        {
            message.Role = "User";
        }
        
        message.Time = DateTime.Now;
        context.Chat!.Messages.Add(message);
    }
}