using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

/// <summary>
/// Uploads a local SKILL.md bundle to a cloud provider's native Skills API and returns
/// the provider-assigned skill id. Implementations are registered per BackendType.
/// </summary>
public interface IProviderSkillUploader
{
    BackendType Backend { get; }

    /// <summary>Returns true when this uploader has the credentials it needs to talk to the provider.</summary>
    bool HasCredentials();

    /// <summary>
    /// Uploads the skill bundle. When <paramref name="existingSkillId"/> is provided and the
    /// provider supports skill versioning (OpenAI), a new version is pushed under the same skill_id;
    /// otherwise a brand new skill is created. Returns the provider reference (skill_id + version).
    /// Throws on transport or auth errors so the coordinator can decide whether to retry or skip.
    /// </summary>
    Task<ProviderSkillReference> UploadAsync(AgentSkill skill, string? existingSkillId = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a previously uploaded skill by id. Best-effort; failures should be logged, not thrown.</summary>
    Task DeleteAsync(string skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns existing skills on the provider keyed by name → reference. Used at startup to repopulate
    /// the cache after it was wiped, so we don't blindly re-upload duplicates.
    /// </summary>
    Task<IReadOnlyDictionary<string, ProviderSkillReference>> ListAsync(CancellationToken cancellationToken = default);
}
