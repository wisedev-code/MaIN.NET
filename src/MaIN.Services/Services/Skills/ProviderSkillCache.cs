using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MaIN.Services.Services.Skills;

public sealed class ProviderSkillCache : IProviderSkillCache, IDisposable
{
    private readonly string _cacheFilePath;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Dispose() => _flushLock.Dispose();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProviderSkillCache(MaINSettings settings, ILogger<ProviderSkillCache>? logger = null)
    {
        _cacheFilePath = ResolvePath(settings.SkillUpload.CacheFilePath);
        _logger = logger ?? NullLogger<ProviderSkillCache>.Instance;
    }

    public bool TryGet(BackendType backend, string skillName, out ProviderSkillReference? reference)
    {
        if (_entries.TryGetValue(Key(backend, skillName), out var entry))
        {
            reference = entry.Reference;
            return true;
        }

        reference = null;
        return false;
    }

    public string? GetBundleHash(BackendType backend, string skillName) =>
        _entries.TryGetValue(Key(backend, skillName), out var entry) ? entry.BundleHash : null;

    public void Set(ProviderSkillReference reference, string bundleHash)
    {
        _entries[Key(reference.Backend, reference.Name)] = new Entry(reference, bundleHash);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _flushLock.WaitAsync(cancellationToken);
        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var snapshot = _entries
                .Select(kv => new CacheEntryDto
                {
                    Backend = kv.Value.Reference.Backend,
                    Name = kv.Value.Reference.Name,
                    SkillId = kv.Value.Reference.SkillId,
                    Version = kv.Value.Reference.Version,
                    BundleHash = kv.Value.BundleHash
                })
                .ToList();

            await using var stream = File.Create(_cacheFilePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_cacheFilePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_cacheFilePath);
            var entries = await JsonSerializer.DeserializeAsync<List<CacheEntryDto>>(stream, JsonOptions, cancellationToken)
                          ?? new List<CacheEntryDto>();

            foreach (var dto in entries)
            {
                var reference = new ProviderSkillReference
                {
                    Backend = dto.Backend,
                    Name = dto.Name,
                    SkillId = dto.SkillId,
                    Version = dto.Version
                };
                _entries[Key(dto.Backend, dto.Name)] = new Entry(reference, dto.BundleHash);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load provider skill cache from '{Path}'. Starting empty.", _cacheFilePath);
        }
    }

    /// <summary>
    /// Stable SHA-256 hash of a skill bundle. Three input shapes are supported:
    /// directory (recursive walk), single file (raw bytes), or synthesized content string.
    /// </summary>
    public static string ComputeBundleHash(string bundlePath)
    {
        if (Directory.Exists(bundlePath))
            return HashDirectory(bundlePath);

        if (File.Exists(bundlePath))
            return HashBytes(File.ReadAllBytes(bundlePath));

        return string.Empty;
    }

    public static string ComputeContentHash(string content) =>
        HashBytes(System.Text.Encoding.UTF8.GetBytes(content));

    private static string HashDirectory(string bundlePath)
    {
        using var sha = SHA256.Create();

        var files = Directory.GetFiles(bundlePath, "*", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(bundlePath, file).Replace('\\', '/');
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(relative);
            sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);

            var content = File.ReadAllBytes(file);
            sha.TransformBlock(content, 0, content.Length, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    private static string HashBytes(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    private static string Key(BackendType backend, string skillName) => $"{backend}::{skillName}";

    private static string ResolvePath(string configured) =>
        Path.IsPathRooted(configured)
            ? configured
            // Resolve against the current working directory so the cache survives bin/Debug
            // rebuilds (AppContext.BaseDirectory points at the build output).
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured));

    private sealed record Entry(ProviderSkillReference Reference, string BundleHash);

    private sealed class CacheEntryDto
    {
        public BackendType Backend { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SkillId { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string BundleHash { get; set; } = string.Empty;
    }
}
