using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.Abstract;

public interface IStepProcessor
{
    Task<Chat> ProcessSteps(AgentContextDocument context,
        AgentDocument agent,
        Knowledge? knowledge,
        Chat chat,
        Func<string, string, string?, string, Task> notifyProgress,
        Func<Chat, Task> updateChat,
        ILogger logger);
}