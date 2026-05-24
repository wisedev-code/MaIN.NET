namespace MaIN.Domain.Configuration;

/// <summary>
/// Per-backend feature predicates. Centralised so SkillComposer, AgentContext, and uploaders
/// agree on which providers expose a native Skills API (OpenAI Responses + Anthropic) and which
/// specific models within each provider actually accept the shell / container.skills payloads.
/// </summary>
public static class BackendCapabilities
{
    public static bool HasNativeSkillsApi(this BackendType backend) =>
        backend is BackendType.OpenAi or BackendType.Anthropic;

    /// <summary>
    /// True when both the backend AND the specific modelId support native Skills API delivery.
    /// OpenAI: Skills + shell tool only ship on gpt-5.5+ (gpt-4o, gpt-4o-mini, gpt-4 reject).
    /// Anthropic: Skills require claude-opus-4-7+ (sonnet / haiku reject).
    /// When this returns false the composer falls back to inline instruction / tools.
    /// </summary>
    public static bool SupportsSkillsApi(this BackendType backend, string? modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return false;

        return backend switch
        {
            BackendType.OpenAi => IsOpenAiSkillsModel(modelId),
            BackendType.Anthropic => IsAnthropicSkillsModel(modelId),
            _ => false
        };
    }

    private static bool IsOpenAiSkillsModel(string modelId)
    {
        // gpt-5.5 and any future ≥5.5 variants. gpt-4* and gpt-3.5* reject the shell tool.
        if (!modelId.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase)) return false;

        var rest = modelId.AsSpan(4);
        var dash = rest.IndexOf('-');
        var versionToken = dash >= 0 ? rest[..dash] : rest;

        if (!double.TryParse(versionToken, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var version))
            return false;

        return version >= 5.5;
    }

    private static bool IsAnthropicSkillsModel(string modelId) =>
        modelId.Contains("opus-4", StringComparison.OrdinalIgnoreCase) ||
        modelId.Contains("opus-5", StringComparison.OrdinalIgnoreCase);
}
