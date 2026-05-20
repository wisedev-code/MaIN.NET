using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

/// <summary>
/// Maps (backend, skill name) → provider-assigned skill id. Persisted on disk to avoid
/// re-uploading bundles between runs. Also tracks bundle content hashes so a changed
/// bundle triggers a re-upload.
/// </summary>
public interface IProviderSkillCache
{
    bool TryGet(BackendType backend, string skillName, out ProviderSkillReference? reference);
    string? GetBundleHash(BackendType backend, string skillName);
    void Set(ProviderSkillReference reference, string bundleHash);
    Task FlushAsync(CancellationToken cancellationToken = default);
    Task LoadAsync(CancellationToken cancellationToken = default);
}
