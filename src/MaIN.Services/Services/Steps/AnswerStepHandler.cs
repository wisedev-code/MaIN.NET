using System.Text.RegularExpressions;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Steps;

public class AnswerStepHandler(ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "ANSWER";

    public async Task<StepResult> Handle(StepContext context)
    {
        await context.NotifyProgress("true", context.Agent.Id, null, context.Agent.CurrentBehaviour);
        var useMemory = context.Arguments.Contains("USE_MEMORY");
        
        var answerCommand = new AnswerCommand { Chat = context.Chat, UseMemory = useMemory };
        var answerResponse = await commandDispatcher.DispatchAsync(answerCommand);
        if (answerResponse == null) 
            throw new Exception("Answer command failed"); //TODO proper candidate for custom exception
        
        
        var filterVal = GetFilter(answerResponse.Content);
        if (!string.IsNullOrEmpty(filterVal))
            context.Chat.Properties.TryAdd("data_filter", filterVal);
        

        answerResponse.Time = DateTime.Now;
        context.Chat.Messages.Add(answerResponse);

        return new StepResult { Chat = context.Chat, RedirectMessage = answerResponse };
    }

    private static string? GetFilter(string? content)
    {
        var pattern = @"filter:?:?\{(.*?)\}";        
        var match = Regex.Match(content!, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }
}