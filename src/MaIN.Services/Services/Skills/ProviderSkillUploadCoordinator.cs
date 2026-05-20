using System.Collections.Concurrent;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MaIN.Services.Services.Skills;

/// <summary>
/// Lazy upload coordinator. <see cref="EnsureUploadedAsync"/> is called from AgentContext before
/// agent composition; the first time a (backend, skill) pair is touched in this process the
/// coordinator (a) reconciles with the provider's existing skill list once, then (b) uploads the
/// bundle if it isn't already there with a matching hash. Orphan ids are deleted on re-upload.
/// </summary>
public sealed class ProviderSkillUploadCoordinator : IDisposable
{
    private readonly ISkillRegistry _registry;
    private readonly IReadOnlyList<IProviderSkillUploader> _uploaders;
    private readonly IProviderSkillCache _cache;
    private readonly MaINSettings _settings;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _cacheLoadLock = new(1, 1);
    private volatile bool _cacheLoaded;
    private bool _dirty;
    private readonly ConcurrentDictionary<BackendType, bool> _reconciledBackends = new();
    // Per-(backend, skill name) lock: prevents two agents created concurrently with the same
    // skill from racing into POST /v1/skills twice and producing duplicates on the provider side.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _uploadLocks = new(StringComparer.OrdinalIgnoreCase);
    private int _disposed;

    public ProviderSkillUploadCoordinator(
        ISkillRegistry registry,
        IEnumerable<IProviderSkillUploader> uploaders,
        IProviderSkillCache cache,
        MaINSettings settings,
        ILogger<ProviderSkillUploadCoordinator>? logger = null)
    {
        _registry = registry;
        // Materialise once — DI may return a fresh IEnumerable per call, and we hit FirstOrDefault
        // on every EnsureUploadedAsync. ToList caps allocations at construction time.
        _uploaders = uploaders.ToList();
        _cache = cache;
        _settings = settings;
        _logger = logger ?? NullLogger<ProviderSkillUploadCoordinator>.Instance;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _cacheLoadLock.Dispose();
        foreach (var gate in _uploadLocks.Values)
            gate.Dispose();
        _uploadLocks.Clear();
    }

    /// <summary>
    /// Loads the disk cache. Called by Bootstrapper.UseMaIN so the very first
    /// <see cref="EnsureUploadedAsync"/> call doesn't pay the disk-read latency.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default) =>
        EnsureCacheLoadedAsync(cancellationToken);

    /// <summary>
    /// Lazy hook — called from AgentContext before composing an agent. Uploads only the skill in
    /// question if needed, leaves other skills untouched. Cache hits are O(1). Does NOT flush the
    /// cache to disk — call <see cref="FlushAsync"/> after a batch of skills to persist changes.
    /// </summary>
    public async Task EnsureUploadedAsync(AgentSkill skill, BackendType backend, CancellationToken cancellationToken = default)
    {
        if (!IsUploadable(skill))
            return;

        var uploader = _uploaders.FirstOrDefault(u => u.Backend == backend);
        if (uploader is null || !uploader.HasCredentials())
            return;

        await EnsureCacheLoadedAsync(cancellationToken);
        await TryReconcileOnceAsync(uploader, backend, cancellationToken);

        // Serialise upload for the same (backend, skill) so two concurrent agents don't both
        // POST /v1/skills for the same name and end up with duplicate skill_ids on the provider.
        var lockKey = $"{backend}::{skill.Name}";
        var gate = _uploadLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (await UploadIfChangedAsync(uploader, skill, cancellationToken))
                _dirty = true;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task TryReconcileOnceAsync(IProviderSkillUploader uploader, BackendType backend, CancellationToken cancellationToken)
    {
        if (!_settings.SkillUpload.ReconcileWithProvider) return;
        if (_reconciledBackends.ContainsKey(backend)) return;

        var ok = await ReconcileFromProviderAsync(uploader, cancellationToken);
        // Only mark this backend as reconciled when the call actually succeeded — otherwise the
        // next EnsureUploadedAsync should get another shot at recovering from a wiped cache.
        if (ok) _reconciledBackends.TryAdd(backend, true);
    }

    /// <summary>
    /// Persists pending cache changes to disk. No-op if nothing changed since the last flush.
    /// Disk failures are swallowed and logged — the cache is a re-upload optimisation, not a
    /// correctness boundary, so a failed flush must never break agent creation.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!_dirty) return;
        try
        {
            await _cache.FlushAsync(cancellationToken);
            _dirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush provider skill cache to disk; in-memory cache remains valid for this process.");
        }
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cacheLoaded) return;
        await _cacheLoadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cacheLoaded) return;
            await _cache.LoadAsync(cancellationToken);
            _cacheLoaded = true;
        }
        finally
        {
            _cacheLoadLock.Release();
        }
    }

    private async Task<bool> UploadIfChangedAsync(IProviderSkillUploader uploader, AgentSkill skill, CancellationToken cancellationToken)
    {
        var prepared = PrepareSkillForUpload(skill);
        try
        {
            var cachedHash = _cache.GetBundleHash(uploader.Backend, skill.Name);
            var hasCached = _cache.TryGet(uploader.Backend, skill.Name, out var cachedRef);

            if (cachedHash == prepared.BundleHash && hasCached)
            {
                _logger.LogDebug("Skill '{Skill}' already uploaded to {Backend} (hash match).", skill.Name, uploader.Backend);
                return false;
            }

            try
            {
                // If we have a cached skill_id, ask the uploader to push a new VERSION under it
                // when the provider supports versioning (OpenAI). Providers without versioning
                // (Anthropic) ignore the parameter and create a new skill — we then clean up the
                // old one via DeleteAsync.
                var reference = await uploader.UploadAsync(prepared.SkillForUpload, cachedRef?.SkillId, cancellationToken);
                _cache.Set(reference, prepared.BundleHash);

                if (hasCached && cachedRef is not null &&
                    !string.Equals(cachedRef.SkillId, reference.SkillId, StringComparison.Ordinal))
                {
                    // skill_id changed → uploader created a brand new skill (no versioning support).
                    // Delete the previous skill so it doesn't linger as an orphan on the provider.
                    _logger.LogInformation("Deleting orphan skill {OldId} on {Backend} after re-uploading '{Skill}'.",
                        cachedRef.SkillId, uploader.Backend, skill.Name);
                    await uploader.DeleteAsync(cachedRef.SkillId, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to upload skill '{Skill}' to {Backend}. Falling back to local composition for this skill.",
                    skill.Name, uploader.Backend);
                return false;
            }
        }
        finally
        {
            prepared.Cleanup();
        }
    }

    private async Task<bool> ReconcileFromProviderAsync(IProviderSkillUploader uploader, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await uploader.ListAsync(cancellationToken);
            var reconciled = 0;
            foreach (var (name, reference) in existing)
            {
                // Only fill in cache gaps — never overwrite a known (hash, id) pair.
                if (_cache.TryGet(uploader.Backend, name, out _)) continue;
                _cache.Set(reference, bundleHash: string.Empty);
                reconciled++;
            }
            if (reconciled > 0)
                _logger.LogInformation("Reconciled {Count} skill(s) from {Backend} into local cache.",
                    reconciled, uploader.Backend);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list existing skills from {Backend}; continuing without reconciliation.", uploader.Backend);
            return false;
        }
    }

    /// <summary>
    /// A skill is uploadable iff it has no code-backed tools (provider can't execute C# delegates
    /// server-side) AND there's content to push: either a bundle on disk (folder OR lone .md file)
    /// or — for code-defined skills — at least an instruction fragment or description that can be
    /// synthesized into a SKILL.md.
    /// </summary>
    public static bool IsUploadable(AgentSkill skill)
    {
        if (skill.Tools.Any(t => t.Execute is not null))
            return false;

        if (!string.IsNullOrEmpty(skill.BundlePath))
            return true;

        return !string.IsNullOrWhiteSpace(skill.InstructionFragment) ||
               !string.IsNullOrWhiteSpace(skill.Description);
    }

    private static PreparedUpload PrepareSkillForUpload(AgentSkill skill)
    {
        if (!string.IsNullOrEmpty(skill.BundlePath))
        {
            return new PreparedUpload(skill, ProviderSkillCache.ComputeBundleHash(skill.BundlePath!), null);
        }

        // Code-defined skill: materialize a temp SKILL.md folder so the rest of the upload pipeline
        // (which expects a BundlePath) can treat it uniformly.
        var content = SynthesizeSkillMarkdown(skill);
        var hash = ProviderSkillCache.ComputeContentHash(content);

        var tempDir = Path.Combine(Path.GetTempPath(), $"main-synth-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "SKILL.md"), content);
        }
        catch
        {
            // Don't leak an empty/partial tempdir if WriteAllText throws after CreateDirectory.
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
            throw;
        }

        var synthSkill = new AgentSkill
        {
            Name = skill.Name,
            Description = skill.Description,
            Version = skill.Version,
            BundlePath = tempDir,
            InstructionFragment = skill.InstructionFragment,
            Tags = skill.Tags,
            Priority = skill.Priority,
            StepPlacement = skill.StepPlacement
        };

        return new PreparedUpload(synthSkill, hash, () =>
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        });
    }

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(LowerCaseNamingConvention.Instance)
        .Build();

    private static string SynthesizeSkillMarkdown(AgentSkill skill)
    {
        // OpenAI Skills require a description in the SKILL.md frontmatter; default to the
        // skill name when the code-defined skill didn't provide one.
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = skill.Name,
            ["description"] = !string.IsNullOrWhiteSpace(skill.Description) ? skill.Description : skill.Name,
            ["version"] = skill.Version
        };

        if (skill.Tags.Length > 0)
            frontmatter["tags"] = skill.Tags;

        var yaml = YamlSerializer.Serialize(frontmatter).TrimEnd();
        var body = skill.InstructionFragment ?? string.Empty;

        return $"---\n{yaml}\n---\n\n{body}\n";
    }

    private sealed record PreparedUpload(AgentSkill SkillForUpload, string BundleHash, Action? CleanupAction)
    {
        public void Cleanup() => CleanupAction?.Invoke();
    }
}
