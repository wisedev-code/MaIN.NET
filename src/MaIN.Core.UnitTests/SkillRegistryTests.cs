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
    public void Constructor_LoadsFromLoaders()
    {
        var skill = MakeSkill("loaded-skill");
        var loader = new Mock<ISkillLoader>();
        loader.Setup(l => l.LoadAll()).Returns([skill]);

        var registry = new SkillRegistry([], [loader.Object], Mock.Of<ILogger<SkillRegistry>>());

        Assert.Equal("loaded-skill", registry.GetSkill("loaded-skill").Name);
    }
}
