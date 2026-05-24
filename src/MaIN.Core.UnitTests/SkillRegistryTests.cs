using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaIN.Core.UnitTests;

public class SkillRegistryTests
{
    private static SkillRegistry MakeRegistry(IEnumerable<AgentSkill>? initial = null)
    {
        var registry = new SkillRegistry([], [], Mock.Of<ILogger<SkillRegistry>>());
        foreach (var skill in initial ?? [])
            registry.Register(skill);
        return registry;
    }

    private static AgentSkill MakeSkill(string name, string[]? tags = null) => new()
    {
        Name = name,
        Steps = [],
        Tags = tags ?? [],
        Priority = 100,
        StepPlacement = SkillStepPlacement.Before
    };

    [Fact]
    public void GetSkill_Registered_ReturnsSkill()
    {
        var registry = MakeRegistry([MakeSkill("calculator")]);

        var result = registry.GetSkill("calculator");

        Assert.Equal("calculator", result.Name);
    }

    [Fact]
    public void GetSkill_CaseInsensitive_ReturnsSkill()
    {
        var registry = MakeRegistry([MakeSkill("Calculator")]);

        var result = registry.GetSkill("CALCULATOR");

        Assert.Equal("Calculator", result.Name);
    }

    [Fact]
    public void GetSkill_NotFound_ThrowsSkillNotFoundException()
    {
        var registry = MakeRegistry();

        Assert.Throws<SkillNotFoundException>(() => registry.GetSkill("unknown"));
    }

    [Fact]
    public void TryGetSkill_Found_ReturnsTrueWithSkill()
    {
        var registry = MakeRegistry([MakeSkill("my-skill")]);

        var found = registry.TryGetSkill("my-skill", out var skill);

        Assert.True(found);
        Assert.NotNull(skill);
    }

    [Fact]
    public void TryGetSkill_NotFound_ReturnsFalseWithNull()
    {
        var registry = MakeRegistry();

        var found = registry.TryGetSkill("unknown", out var skill);

        Assert.False(found);
        Assert.Null(skill);
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var registry = MakeRegistry([MakeSkill("a"), MakeSkill("b"), MakeSkill("c")]);

        Assert.Equal(3, registry.GetAll().Count);
    }

    [Fact]
    public void GetByTag_ReturnsOnlyMatchingSkills()
    {
        var registry = MakeRegistry([
            MakeSkill("web-search", ["web", "search"]),
            MakeSkill("rag-expert", ["knowledge", "rag"]),
            MakeSkill("journalist", ["web", "persona"])
        ]);

        var results = registry.GetByTag("web");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, s => s.Name == "web-search");
        Assert.Contains(results, s => s.Name == "journalist");
    }

    [Fact]
    public void GetByTag_NoMatch_ReturnsEmpty()
    {
        var registry = MakeRegistry([MakeSkill("some-skill", ["tag-a"])]);

        var results = registry.GetByTag("tag-b");

        Assert.Empty(results);
    }

    [Fact]
    public void GetByTag_CaseInsensitive_ReturnsMatch()
    {
        var registry = MakeRegistry([MakeSkill("my-skill", ["Web"])]);

        var results = registry.GetByTag("web");

        Assert.Single(results);
    }

    [Fact]
    public void Register_Duplicate_OverwritesPrevious()
    {
        var registry = MakeRegistry([MakeSkill("my-skill")]);
        var updated = new AgentSkill
        {
            Name = "my-skill",
            Description = "updated",
            Steps = [],
            Tags = [],
            Priority = 100,
            StepPlacement = SkillStepPlacement.Before
        };

        registry.Register(updated);

        Assert.Equal("updated", registry.GetSkill("my-skill").Description);
    }

    [Fact]
    public void Constructor_LoadsFromProviders()
    {
        var skill = MakeSkill("provided-skill");
        var provider = new Mock<IAgentSkillProvider>();
        provider.Setup(p => p.GetSkill()).Returns(skill);

        var registry = new SkillRegistry([provider.Object], [], Mock.Of<ILogger<SkillRegistry>>());

        Assert.Equal("provided-skill", registry.GetSkill("provided-skill").Name);
    }

    [Fact]
    public void GetAllExcludingBuiltIn_FiltersOnlyBuiltInProviders()
    {
        var builtIn = MakeSkill("web-search");
        var userProvider = MakeSkill("user-provider-skill");
        var folderSkill = MakeSkill("folder-skill");

        var builtInProvider = new Mock<IBuiltInAgentSkillProvider>();
        builtInProvider.Setup(p => p.GetSkill()).Returns(builtIn);

        var customProvider = new Mock<IAgentSkillProvider>();
        customProvider.Setup(p => p.GetSkill()).Returns(userProvider);

        var loader = new Mock<ISkillLoader>();
        loader.Setup(l => l.LoadAll()).Returns([folderSkill]);

        var registry = new SkillRegistry(
            [builtInProvider.Object, customProvider.Object],
            [loader.Object],
            Mock.Of<ILogger<SkillRegistry>>());

        Assert.Equal(3, registry.GetAll().Count);

        var filtered = registry.GetAllExcludingBuiltIn();
        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, s => s.Name == "user-provider-skill");
        Assert.Contains(filtered, s => s.Name == "folder-skill");
        Assert.DoesNotContain(filtered, s => s.Name == "web-search");
    }

    [Fact]
    public void AllBundledSkillProvidersInMaINCoreImplementBuiltInMarker()
    {
        // Guard: every IAgentSkillProvider shipped in MaIN.Core must also implement
        // IBuiltInAgentSkillProvider so .WithAllSkills() filters it out by default.
        var assembly = typeof(MaIN.Core.Hub.Skills.WebSearchSkillProvider).Assembly;

        var bundledProviders = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IAgentSkillProvider).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(bundledProviders);

        var missing = bundledProviders
            .Where(t => !typeof(IBuiltInAgentSkillProvider).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Bundled IAgentSkillProvider(s) missing IBuiltInAgentSkillProvider marker: {string.Join(", ", missing)}");
    }

    [Fact]
    public void GetAllExcludingBuiltIn_DirectRegisterCallsAreNotTreatedAsBuiltIn()
    {
        var registry = new SkillRegistry([], [], Mock.Of<ILogger<SkillRegistry>>());
        registry.Register(MakeSkill("manually-registered"));

        var filtered = registry.GetAllExcludingBuiltIn();

        Assert.Single(filtered);
        Assert.Equal("manually-registered", filtered[0].Name);
    }

    [Fact]
    public void Constructor_LoadsFromLoaders()
    {
        var skill = MakeSkill("loaded-skill");
        var loader = new Mock<ISkillLoader>();
        loader.Setup(l => l.LoadAll()).Returns([skill]);

        var registry = new SkillRegistry([], [loader.Object], Mock.Of<ILogger<SkillRegistry>>());

        Assert.Equal("loaded-skill", registry.GetSkill("loaded-skill").Name);
    }
}
