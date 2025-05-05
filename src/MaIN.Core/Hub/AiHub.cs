using LLama.Native;
using MaIN.Core.Hub.Contexts;
using MaIN.Core.Interfaces;
using MaIN.Services.Services.Abstract;

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

    public static ChatContext Chat() => new(Services.ChatService);
    public static AgentContext Agent() => new(Services.AgentService);
    public static FlowContext Flow() => new(Services.FlowService, Services.AgentService);

    public abstract class Extensions
    {
        public static void DisableLLamaLogs()
        {
            NativeLogConfig.llama_log_set((_,_) => {});
        }
        
        public static void DisableNotificationsLogs()
        {
            INotificationService.Disable = true;
        }
    }
}

