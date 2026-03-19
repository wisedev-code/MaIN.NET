using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.Abstract;

public interface IStepProcessor
{
    Task<Chat> ProcessSteps(AgentConfig context,
        Agent agent,
        Knowledge? knowledge,
        Chat chat,
        Func<LLMTokenValue, Task>? callback,
        Func<ToolInvocation, Task>? callbackTool,
        Func<string, string, string?, string, string, Task> notifyProgress,
        Func<Chat, Task> updateChat,
        ILogger logger);
}
