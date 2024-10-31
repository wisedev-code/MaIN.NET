using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Abstract;

public interface IStepHandler
{
    string StepName { get; }
    string[] SupportedSteps => [StepName]; // Default implementation returns just the main step name
    Task<StepResult> Handle(StepContext context);
}