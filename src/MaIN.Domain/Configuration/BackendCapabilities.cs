namespace MaIN.Domain.Configuration;

/// <summary>
/// Per-backend feature predicates. Centralised so SkillComposer, AgentContext, and uploaders
/// agree on which providers expose a native Skills API (OpenAI Responses + Anthropic).
/// </summary>
public static class BackendCapabilities
{
    public static bool HasNativeSkillsApi(this BackendType backend) =>
        backend is BackendType.OpenAi or BackendType.Anthropic;
}
