using MaIN.Core.Hub.Contexts;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Contexts.Interfaces.ChatContext;
using MaIN.Core.Hub.Contexts.Interfaces.FlowContext;
using MaIN.Core.Hub.Contexts.Interfaces.McpContext;
using MaIN.Core.Hub.Contexts.Interfaces.ModelContext;
using MaIN.Core.Interfaces;
using MaIN.Domain.Configuration;

namespace MaIN.Core.Hub;

internal sealed class MaINHub(
    IAIHubServices services,
    MaINSettings settings,
    IHttpClientFactory httpClientFactory) : IMaINHub
{
    public IChatBuilderEntryPoint Chat() => new ChatContext(services.ChatService, Model());

    public IAgentBuilderEntryPoint Agent() => new AgentContext(
        services.AgentService,
        services.SkillRegistry,
        services.SkillComposer,
        services.UploadCoordinator,
        Model());

    public IFlowContext Flow() => new FlowContext(services.FlowService, services.AgentService);

    public IModelContext Model() => new ModelContext(settings, httpClientFactory);

    public IMcpContext Mcp() => new McpContext(services.McpService);
}
