using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.Interfaces;

public interface IAIHubServices
{
    IChatService ChatService { get; }
    IAgentService AgentService { get; }
    IAgentFlowService FlowService { get; }
    IMcpService McpService { get; }
    ISkillRegistry SkillRegistry { get; }
    ISkillComposer SkillComposer { get; }
    ProviderSkillUploadCoordinator? UploadCoordinator { get; }
}