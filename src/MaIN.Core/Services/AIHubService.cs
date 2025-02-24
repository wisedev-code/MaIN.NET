using MaIN.Core.Interfaces;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Services;

public class AIHubServices(
    IChatService chatService,
    IAgentService agentService,
    IAgentFlowService flowService)
    : IAIHubServices
{
    public IChatService ChatService { get; } = chatService;
    public IAgentService AgentService { get; } = agentService;
    public IAgentFlowService FlowService { get; } = flowService;
}