using MaIN.Core.Hub.Contexts;
using MaIN.Core.Interfaces;

namespace MaIN.Core.Hub;

public static class AIHub
{
    private static IAIHubServices? _services;

    internal static void Initialize(IAIHubServices services)
    {
        _services = services;
    }

    private static IAIHubServices Services =>
        _services ??
        throw new InvalidOperationException(
            "AIHub has not been initialized. Make sure to call AddAIHub() in your service configuration.");

    public static ChatContext Chat() => new ChatContext(Services.ChatService);
    public static AgentContext Agent() => new AgentContext(Services.AgentService);
    public static FlowContext Flow() => new FlowContext(Services.FlowService, Services.AgentService);
}

