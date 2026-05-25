using System.Net;
using MaIN.Domain.Configuration;

namespace MaIN.Domain.Exceptions.Skills;

/// <summary>
/// Thrown by <c>SkillComposer</c> when <c>MaINSettings.SkillUpload.RequireNativeSkillsApi</c> is
/// enabled and an uploadable skill cannot be routed through the provider's native Skills API —
/// because the backend has no Skills API, the selected model rejects it, or the skill_id is
/// missing from the cache after an upload attempt.
/// </summary>
public sealed class SkillNotSupportedException : MaINCustomException
{
    public string SkillName { get; }
    public BackendType Backend { get; }
    public string? ModelId { get; }
    public string Reason { get; }

    public SkillNotSupportedException(string skillName, BackendType backend, string? modelId, string reason)
        : base(
            $"Skill '{skillName}' cannot be routed through native Skills API on {backend} " +
            $"(model: {modelId ?? "<unset>"}): {reason}. " +
            "Disable MaINSettings.SkillUpload.RequireNativeSkillsApi or use a backend/model that supports Skills.")
    {
        SkillName = skillName;
        Backend = backend;
        ModelId = modelId;
        Reason = reason;
    }

    public override string PublicErrorMessage => "Skill cannot be routed through native Skills API.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
