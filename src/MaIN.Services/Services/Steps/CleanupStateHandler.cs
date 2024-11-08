using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps;

public class CleanupStepHandler : IStepHandler
{
    public string StepName => "CLEANUP";

    public Task<StepResult> Handle(StepContext context)
    {
        AgentStateManager.ClearState(context.Agent, context.Chat!);
        return Task.FromResult(new StepResult { Chat = context.Chat });
    }
}