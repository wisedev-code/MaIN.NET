using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class StepProcessor : IStepProcessor
{
    private readonly Dictionary<string, IStepHandler> _stepHandlers;

    public StepProcessor(IEnumerable<IStepHandler> stepHandlers)
    {
        _stepHandlers = new Dictionary<string, IStepHandler>();
        
        foreach (var handler in stepHandlers)
        {
            foreach (var supportedStep in handler.SupportedSteps)
            {
                _stepHandlers[supportedStep] = handler;
            }
        }
    }

    public async Task<Chat> ProcessSteps(
        AgentContextDocument context,
        AgentDocument agent,
        Chat chat,
        Func<string, string, string?, string, Task> notifyProgress,
        Func<Chat, Task> updateChat,
        ILogger logger)
    {
        Message redirectMessage = chat?.Messages?.Last()!;
        var tagsToReplaceWithFilter = new List<string>();
        var index = 0;
        foreach (var step in context.Steps)
        {
            logger.LogInformation("Processing step: {Step} on agent {agent}", step, agent.Name);
            
            var (stepName, arguments) = ParseStep(step);
            var handler = GetStepHandler(stepName);

            var stepContext = new StepContext
            {
                Agent = agent,
                Chat = chat,
                RedirectMessage = redirectMessage,
                TagsToReplaceWithFilter = tagsToReplaceWithFilter,
                Arguments = arguments,
                NotifyProgress = notifyProgress,
                UpdateChat = updateChat,
                StepName = stepName
            };

            var result = await handler.Handle(stepContext);

            if (stepName != "REDIRECT")
            {
                redirectMessage = result.RedirectMessage ?? redirectMessage;
            }
            
            chat = result.Chat;

            await updateChat(chat!);
            index++;
        }

        await CleanupBehaviors(agent, tagsToReplaceWithFilter);
        
        return chat;
    }

    private static (string Name, string[] Arguments) ParseStep(string step)
    {
        var parts = step.Split('+');
        return (parts[0], parts.Skip(1).ToArray());
    }

    private IStepHandler GetStepHandler(string stepName) =>
        _stepHandlers.TryGetValue(stepName, out var handler)
            ? handler
            : throw new InvalidOperationException($"Unknown step: {stepName}");

    private static Task CleanupBehaviors(AgentDocument agent, List<string> tagsToReplaceWithFilter)
    {
        foreach (var key in agent.Behaviours.Keys.ToList())
        {
            agent.Behaviours[key] = tagsToReplaceWithFilter.Aggregate(
                agent.Behaviours[key],
                (current, tag) => current.Replace(tag, "@filter@"));
        }

        return Task.CompletedTask;
    }
}