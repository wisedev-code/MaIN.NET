using MaIN.Core.Hub.Contexts;
using MaIN.Core.Interfaces;
using MaIN.Domain.Configuration;

namespace MaIN.Core.Hub;

internal sealed class MaINHub(
    IAIHubServices services,
    MaINSettings settings,
    IHttpClientFactory httpClientFactory) : IMaINHub
{
    public ChatContext Chat() => new(services.ChatService);

    public AgentContext Agent() => new(
        services.AgentService,
        services.SkillRegistry,
        services.SkillComposer,
        services.UploadCoordinator);

    public FlowContext Flow() => new(services.FlowService, services.AgentService);

    public ModelContext Model() => new(settings, httpClientFactory);

    public McpContext Mcp() => new(services.McpService);
}
