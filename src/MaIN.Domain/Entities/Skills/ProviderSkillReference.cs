using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities.Skills;

/// <summary>
/// Reference to a skill that has been uploaded to a cloud provider's native Skills API.
/// Carried on Chat/Agent so the LLM service can attach it as a provider-side skill reference
/// (e.g. OpenAI Responses environment.skills, Anthropic container.skills) rather than
/// inlining the skill's instruction fragment or tool schemas into the prompt.
/// </summary>
public class ProviderSkillReference
{
    public required string Name { get; init; }
    public required string SkillId { get; init; }
    public string? Version { get; init; }
    public required BackendType Backend { get; init; }
}
