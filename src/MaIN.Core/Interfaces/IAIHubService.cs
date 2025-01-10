using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Interfaces;

internal interface IAIHubServices
{
    IChatService ChatService { get; }
    IAgentService AgentService { get; }
    IAgentFlowService FlowService { get; }
}