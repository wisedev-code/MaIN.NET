using MaIN.Core.Interfaces;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.Services;

public sealed class AIHubServices(
    IChatService chatService,
    IAgentService agentService,
    IAgentFlowService flowService,
    IMcpService mcpService,
    ISkillRegistry skillRegistry,
    ISkillComposer skillComposer,
    ProviderSkillUploadCoordinator? uploadCoordinator = null)
    : IAIHubServices
{
    public IChatService ChatService { get; } = chatService;
    public IAgentService AgentService { get; } = agentService;
    public IAgentFlowService FlowService { get; } = flowService;
    public IMcpService McpService { get; } = mcpService;
    public ISkillRegistry SkillRegistry { get; } = skillRegistry;
    public ISkillComposer SkillComposer { get; } = skillComposer;
    public ProviderSkillUploadCoordinator? UploadCoordinator { get; } = uploadCoordinator;
}
