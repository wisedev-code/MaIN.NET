using LLama.Native;
using MaIN.Core.Hub.Contexts;
using MaIN.Core.Interfaces;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub;

public static class AIHub
{
    private static IAIHubServices? _services;
    private static MaINSettings _settings = null!;
    private static IHttpClientFactory _httpClientFactory = null!;

    internal static bool IsInitialized => _services is not null;

    /// <summary>
    /// Returns skills that should survive a re-initialization: folder-loaded skills and
    /// user-registered skills (via direct Register() or non-built-in IAgentSkillProvider).
    /// Bundled built-in skills are excluded because the fresh DI container re-provides them.
    /// </summary>
    internal static IReadOnlyList<AgentSkill>? GetCurrentSkills() =>
        _services?.SkillRegistry.GetAllExcludingBuiltIn();

    internal static void Initialize(IAIHubServices services,
        MaINSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _services = services;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    private static IAIHubServices Services =>
        _services ??
        throw new AIHubNotInitializedException();

    public static ModelContext Model() => new(_settings, _httpClientFactory);
    public static ChatContext Chat() => new(Services.ChatService);
    public static AgentContext Agent() => new(Services.AgentService, Services.SkillRegistry, Services.SkillComposer);
    public static FlowContext Flow() => new(Services.FlowService, Services.AgentService);
    public static McpContext Mcp() => new(Services.McpService);

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

