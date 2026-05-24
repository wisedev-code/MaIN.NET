using System.IO;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.UnitTests;

public class ProviderSkillCacheTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MaINSettings _settings;

    public ProviderSkillCacheTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"main-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settings = new MaINSettings
        {
            SkillUpload = new SkillUploadSettings
            {
                CacheFilePath = Path.Combine(_tempDir, "skills-cache.json")
            }
        };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public async Task Set_Then_Flush_Then_Load_RoundTripsReference()
    {
        var cache = new ProviderSkillCache(_settings);

        cache.Set(new ProviderSkillReference
        {
            Name = "code-review",
            SkillId = "sk_abc",
            Version = "3",
            Backend = BackendType.OpenAi
        }, bundleHash: "deadbeef");

        await cache.FlushAsync();

        var cache2 = new ProviderSkillCache(_settings);
        await cache2.LoadAsync();

        Assert.True(cache2.TryGet(BackendType.OpenAi, "code-review", out var reference));
        Assert.NotNull(reference);
        Assert.Equal("sk_abc", reference!.SkillId);
        Assert.Equal("3", reference.Version);
        Assert.Equal("deadbeef", cache2.GetBundleHash(BackendType.OpenAi, "code-review"));
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var cache = new ProviderSkillCache(_settings);

        Assert.False(cache.TryGet(BackendType.OpenAi, "ghost", out var reference));
        Assert.Null(reference);
    }

    [Fact]
    public void Set_DifferentBackends_KeyedSeparately()
    {
        var cache = new ProviderSkillCache(_settings);

        cache.Set(new ProviderSkillReference
            { Name = "shared", SkillId = "openai-id", Backend = BackendType.OpenAi },
            bundleHash: "h1");
        cache.Set(new ProviderSkillReference
            { Name = "shared", SkillId = "anthropic-id", Backend = BackendType.Anthropic },
            bundleHash: "h2");

        Assert.True(cache.TryGet(BackendType.OpenAi, "shared", out var openAi));
        Assert.True(cache.TryGet(BackendType.Anthropic, "shared", out var anthropic));
        Assert.Equal("openai-id", openAi!.SkillId);
        Assert.Equal("anthropic-id", anthropic!.SkillId);
    }

    [Fact]
    public void ComputeBundleHash_OnDirectory_IncludesFileNamesAndContent()
    {
        var bundleDir = Path.Combine(_tempDir, "bundle-a");
        Directory.CreateDirectory(bundleDir);
        File.WriteAllText(Path.Combine(bundleDir, "SKILL.md"), "hello");
        File.WriteAllText(Path.Combine(bundleDir, "extra.md"), "world");

        var hash1 = ProviderSkillCache.ComputeBundleHash(bundleDir);

        // Same content → same hash
        Assert.Equal(hash1, ProviderSkillCache.ComputeBundleHash(bundleDir));

        // Change a file → hash changes
        File.WriteAllText(Path.Combine(bundleDir, "SKILL.md"), "changed");
        var hash2 = ProviderSkillCache.ComputeBundleHash(bundleDir);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeBundleHash_OnLoneFile_UsesFileBytes()
    {
        var filePath = Path.Combine(_tempDir, "lone.md");
        File.WriteAllText(filePath, "hello");
        var hash1 = ProviderSkillCache.ComputeBundleHash(filePath);

        File.WriteAllText(filePath, "world");
        var hash2 = ProviderSkillCache.ComputeBundleHash(filePath);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeContentHash_StableForSameString()
    {
        var hash1 = ProviderSkillCache.ComputeContentHash("same");
        var hash2 = ProviderSkillCache.ComputeContentHash("same");
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, ProviderSkillCache.ComputeContentHash("different"));
    }
}
