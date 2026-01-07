using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Interfaces;

public interface IAIHubServices
{
    IChatService ChatService { get; }
    IAgentService AgentService { get; }
    IAgentFlowService FlowService { get; }
    IMcpService McpService { get; }
}