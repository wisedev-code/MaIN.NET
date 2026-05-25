using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Domain.Models;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaIN.Core.UnitTests;

public class SkillComposerTests
{
    private readonly SkillComposer _composer = new(Mock.Of<ILogger<SkillComposer>>());

    private static Agent MakeAgent(string? modelId = null, List<string>? steps = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        CurrentBehaviour = "Default",
        Behaviours = [],
        Config = new AgentConfig { Steps = steps ?? ["ANSWER"] },
        Model = modelId ?? ""
    };

    private static AgentSkill MakeSkill(
        string name = "test-skill",
        List<string>? steps = null,
        SkillStepPlacement placement = SkillStepPlacement.Before,
        string? instructionFragment = null,
        SkillMcpDefinition? mcp = null,
        SkillSourceDefinition? source = null,
        List<SkillToolDefinition>? tools = null,
        Dictionary<string, string>? behaviours = null,
        int priority = 100) => new AgentSkill
    {
        Name = name,
        Steps = steps ?? [],
        StepPlacement = placement,
        InstructionFragment = instructionFragment,
        Mcp = mcp,
        Source = source,
        Tools = tools ?? [],
        Behaviours = behaviours ?? [],
        Priority = priority
    };

    // --- Steps ---

    [Fact]
    public void MergeSteps_Replace_OverwritesAgentSteps()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var skill = MakeSkill(steps: ["MCP", "ANSWER"], placement: SkillStepPlacement.Replace);

        _composer.Apply(agent, [skill]);

        Assert.Equal(["MCP", "ANSWER"], agent.Config.Steps);
    }

    [Fact]
    public void LogFinalPipeline_KeepsLegitStepQualifiersVisible()
    {
        var loggerMock = new Mock<ILogger<SkillComposer>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);
        var composer = new SkillComposer(loggerMock.Object);

        var agent = MakeAgent(steps: ["BECOME+Journalist", "ANSWER+USE_KNOWLEDGE"]);
        var skill = MakeSkill(name: "journalist", steps: ["BECOME+Journalist"], placement: SkillStepPlacement.Before);

        composer.Apply(agent, [skill]);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("BECOME+Journalist") &&
                    v.ToString()!.Contains("ANSWER+USE_KNOWLEDGE")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogFinalPipeline_RedactsUrlAndTokenStepArgs()
    {
        var loggerMock = new Mock<ILogger<SkillComposer>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);
        var composer = new SkillComposer(loggerMock.Object);

        var agent = MakeAgent(steps: [
            "FETCH+https://api.example.com/secret",
            "AUTH+sk-abc123def456",
            "BECOME+Journalist"
        ]);

        composer.Apply(agent, [MakeSkill(steps: ["NOOP"], placement: SkillStepPlacement.Before)]);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    !v.ToString()!.Contains("https://api.example.com") &&
                    !v.ToString()!.Contains("sk-abc123def456") &&
                    v.ToString()!.Contains("BECOME+Journalist") &&
                    v.ToString()!.Contains("FETCH+…") &&
                    v.ToString()!.Contains("AUTH+…")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void MergeSteps_TwoReplaceSkills_ThrowsConflict()
    {
        var agent = MakeAgent();
        var first = MakeSkill(name: "rag-expert", steps: ["ANSWER+USE_KNOWLEDGE"], placement: SkillStepPlacement.Replace);
        var second = MakeSkill(name: "funfact-writer", steps: ["MCP"], placement: SkillStepPlacement.Replace);

        var ex = Assert.Throws<SkillConflictException>(() => _composer.Apply(agent, [first, second]));
        Assert.Contains("rag-expert", ex.Message);
        Assert.Contains("funfact-writer", ex.Message);
    }

    [Fact]
    public void MergeSteps_ReplaceMixedWithBeforeAfter_WarnsAndDiscardsSiblingSteps()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var loggerMock = new Mock<ILogger<SkillComposer>>();
        var composer = new SkillComposer(loggerMock.Object);

        var replaceSkill = MakeSkill(name: "funfact-writer", steps: ["MCP"], placement: SkillStepPlacement.Replace);
        var beforeSkill = MakeSkill(name: "code-review", steps: ["ANSWER"], placement: SkillStepPlacement.Before);

        composer.Apply(agent, [replaceSkill, beforeSkill]);

        Assert.Equal(["MCP"], agent.Config.Steps);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("funfact-writer") && v.ToString()!.Contains("code-review")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MergeSteps_Before_PrependsAndDeduplicates()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var skill = MakeSkill(steps: ["FETCH", "ANSWER"], placement: SkillStepPlacement.Before);

        _composer.Apply(agent, [skill]);

        Assert.Equal(["FETCH", "ANSWER"], agent.Config.Steps);
    }

    [Fact]
    public void MergeSteps_After_AppendsAndDeduplicates()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var skill = MakeSkill(steps: ["SUMMARIZE"], placement: SkillStepPlacement.After);

        _composer.Apply(agent, [skill]);

        Assert.Equal(["ANSWER", "SUMMARIZE"], agent.Config.Steps);
    }

    [Fact]
    public void MergeSteps_ReplaceWinsOverBeforeAfter()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var before = MakeSkill("before", steps: ["FETCH"], placement: SkillStepPlacement.Before, priority: 10);
        var replace = MakeSkill("replace", steps: ["MCP"], placement: SkillStepPlacement.Replace, priority: 20);

        _composer.Apply(agent, [before, replace]);

        Assert.Equal(["MCP"], agent.Config.Steps);
    }

    // --- Tools ---

    [Fact]
    public void MergeTools_DuplicateName_ThrowsSkillConflictException()
    {
        var agent = MakeAgent();
        var tool = new SkillToolDefinition { Name = "calculator", Description = "calc", Parameters = new { } };
        var skillA = MakeSkill("skill-a", tools: [tool]);
        var skillB = MakeSkill("skill-b", tools: [new SkillToolDefinition { Name = "calculator", Description = "calc2", Parameters = new { } }]);

        Assert.Throws<SkillConflictException>(() => _composer.Apply(agent, [skillA, skillB]));
    }

    // --- Source ---

    [Fact]
    public void MergeSource_TwoSkillsWithSource_ThrowsSkillConflictException()
    {
        var agent = MakeAgent();
        var src = new SkillSourceDefinition
        {
            Details = new AgentWebSourceDetails { Url = "https://example.com" },
            Type = AgentSourceType.Web
        };
        var skillA = MakeSkill("skill-a", source: src);
        var skillB = MakeSkill("skill-b", source: new SkillSourceDefinition
        {
            Details = new AgentWebSourceDetails { Url = "https://other.com" },
            Type = AgentSourceType.Web
        });

        Assert.Throws<SkillConflictException>(() => _composer.Apply(agent, [skillA, skillB]));
    }

    [Fact]
    public void MergeSource_AgentAlreadyHasSource_ThrowsSkillConflictException()
    {
        var agent = MakeAgent();
        agent.Config.Source = new AgentSource
        {
            Details = new AgentWebSourceDetails { Url = "https://existing.com" },
            Type = AgentSourceType.Web
        };
        var skill = MakeSkill(source: new SkillSourceDefinition
        {
            Details = new AgentWebSourceDetails { Url = "https://new.com" },
            Type = AgentSourceType.Web
        });

        Assert.Throws<SkillConflictException>(() => _composer.Apply(agent, [skill]));
    }

    // --- MCP ---

    [Fact]
    public void MergeMcp_SetsModelAndBackendFromAgent()
    {
        var agent = MakeAgent(modelId: Models.OpenAi.Gpt4oMini);
        var skill = MakeSkill(mcp: new SkillMcpDefinition
        {
            Command = "npx",
            Arguments = ["-y", "@mcp/server-test"],
            Environment = [],
            Properties = []
        });

        _composer.Apply(agent, [skill]);

        Assert.NotNull(agent.Config.McpConfig);
        Assert.Equal(Models.OpenAi.Gpt4oMini, agent.Config.McpConfig!.Model);
        Assert.Equal(BackendType.OpenAi, agent.Config.McpConfig.Backend);
    }

    [Fact]
    public void MergeMcp_TwoSkillsWithMcp_ThrowsSkillConflictException()
    {
        var agent = MakeAgent();
        var mcpDef = new SkillMcpDefinition { Command = "npx", Arguments = [], Environment = [], Properties = [] };

        Assert.Throws<SkillConflictException>(() =>
            _composer.Apply(agent, [MakeSkill("a", mcp: mcpDef), MakeSkill("b", mcp: mcpDef)]));
    }

    [Fact]
    public void MergeMcp_AgentAlreadyHasMcpConfig_SkipsSkillMcp()
    {
        var agent = MakeAgent();
        agent.Config.McpConfig = new MaIN.Domain.Entities.Mcp
        {
            Name = "existing", Command = "docker", Arguments = [], Model = "existing-model"
        };
        var skill = MakeSkill(mcp: new SkillMcpDefinition { Command = "npx", Arguments = [], Environment = [], Properties = [] });

        _composer.Apply(agent, [skill]);

        // existing config not overwritten
        Assert.Equal("docker", agent.Config.McpConfig.Command);
    }

    // --- Behaviours ---

    [Fact]
    public void MergeBehaviours_MergesAllFromSkills()
    {
        var agent = MakeAgent();
        var skill = MakeSkill(behaviours: new Dictionary<string, string>
        {
            ["Journalist"] = "Write a newsletter.",
            ["Critic"] = "Be critical."
        });

        _composer.Apply(agent, [skill]);

        Assert.Equal("Write a newsletter.", agent.Behaviours["Journalist"]);
        Assert.Equal("Be critical.", agent.Behaviours["Critic"]);
    }

    // --- InstructionFragment ---

    [Fact]
    public void MergeInstructionFragments_AppendsToExistingInstruction()
    {
        var agent = MakeAgent();
        agent.Config.Instruction = "Base instruction.";
        var skill = MakeSkill(instructionFragment: "Search the web carefully.");

        _composer.Apply(agent, [skill]);

        Assert.Equal("Base instruction.\n\nSearch the web carefully.", agent.Config.Instruction);
    }

    [Fact]
    public void MergeInstructionFragments_MultipleSkillsConcatenatedByPriority()
    {
        var agent = MakeAgent();
        agent.Config.Instruction = null;
        var skillA = MakeSkill("a", instructionFragment: "Fragment A.", priority: 10);
        var skillB = MakeSkill("b", instructionFragment: "Fragment B.", priority: 20);

        _composer.Apply(agent, [skillA, skillB]);

        Assert.Equal("Fragment A.\n\nFragment B.", agent.Config.Instruction);
    }

    // --- No-op ---

    [Fact]
    public void Apply_EmptySkillList_AgentUnchanged()
    {
        var agent = MakeAgent(steps: ["ANSWER"]);
        var originalInstruction = agent.Config.Instruction;

        _composer.Apply(agent, []);

        Assert.Equal(["ANSWER"], agent.Config.Steps);
        Assert.Equal(originalInstruction, agent.Config.Instruction);
    }

    // --- Hybrid provider-skill routing ---

    [Fact]
    public void Apply_CloudBackendWithCachedSkill_RoutesAsProviderReferenceAndSkipsInlineInstruction()
    {
        var cacheMock = new Mock<IProviderSkillCache>();
        var reference = new ProviderSkillReference
        {
            Name = "report-writer",
            SkillId = "skill_abc123",
            Backend = BackendType.OpenAi
        };
        cacheMock
            .Setup(c => c.TryGet(BackendType.OpenAi, "report-writer", out It.Ref<ProviderSkillReference?>.IsAny))
            .Returns(new TryGetCallback((BackendType _, string _, out ProviderSkillReference? r) =>
            {
                r = reference;
                return true;
            }));

        var composer = new SkillComposer(Mock.Of<ILogger<SkillComposer>>(), cacheMock.Object);

        // gpt-5.5 is the minimum OpenAI model that accepts the Skills shell tool.
        var agent = MakeAgent(modelId: "gpt-5.5");
        var skill = MakeSkill("report-writer", instructionFragment: "Draft reports.");
        // Simulate a bundle on disk to make it uploadable.
        skill = new AgentSkill
        {
            Name = skill.Name,
            Steps = skill.Steps,
            StepPlacement = skill.StepPlacement,
            InstructionFragment = skill.InstructionFragment,
            Tools = skill.Tools,
            Behaviours = skill.Behaviours,
            Priority = skill.Priority,
            BundlePath = "/tmp/fake-bundle"
        };

        composer.Apply(agent, [skill], BackendType.OpenAi);

        Assert.Single(agent.ProviderSkillReferences);
        Assert.Equal("skill_abc123", agent.ProviderSkillReferences[0].SkillId);
        Assert.Null(agent.Config.Instruction);
    }

    [Fact]
    public void Apply_LocalBackend_KeepsExistingInlineCompositionEvenForUploadableSkill()
    {
        var cacheMock = new Mock<IProviderSkillCache>();
        var composer = new SkillComposer(Mock.Of<ILogger<SkillComposer>>(), cacheMock.Object);

        var agent = MakeAgent();
        var skill = new AgentSkill
        {
            Name = "report-writer",
            InstructionFragment = "Draft reports.",
            BundlePath = "/tmp/fake-bundle"
        };

        composer.Apply(agent, [skill], BackendType.Self);

        Assert.Empty(agent.ProviderSkillReferences);
        Assert.Equal("Draft reports.", agent.Config.Instruction);
        cacheMock.Verify(c => c.TryGet(It.IsAny<BackendType>(), It.IsAny<string>(), out It.Ref<ProviderSkillReference?>.IsAny), Times.Never);
    }

    private delegate bool TryGetCallback(BackendType backend, string name, out ProviderSkillReference? reference);

    // --- RequireNativeSkillsApi (strict) ---

    private static AgentSkill MakeUploadableSkill(string name = "report-writer") => new()
    {
        Name = name,
        InstructionFragment = "Draft reports.",
        Tools = [],
        BundlePath = "/tmp/fake-bundle"
    };

    private static MaINSettings MakeSettings(bool strict) => new()
    {
        SkillUpload = new SkillUploadSettings { RequireNativeSkillsApi = strict }
    };

    [Fact]
    public void Strict_UploadableSkill_OnUnsupportedBackend_Throws()
    {
        var composer = new SkillComposer(
            Mock.Of<ILogger<SkillComposer>>(),
            Mock.Of<IProviderSkillCache>(),
            MakeSettings(strict: true));

        var agent = MakeAgent(modelId: "gemini-2.5-pro");
        var skill = MakeUploadableSkill();

        var ex = Assert.Throws<SkillNotSupportedException>(
            () => composer.Apply(agent, [skill], BackendType.Gemini));

        Assert.Equal("report-writer", ex.SkillName);
        Assert.Equal(BackendType.Gemini, ex.Backend);
        Assert.Contains("backend has no Skills API", ex.Reason);
    }

    [Fact]
    public void Strict_UploadableSkill_OnUnsupportedOpenAiModel_Throws()
    {
        var composer = new SkillComposer(
            Mock.Of<ILogger<SkillComposer>>(),
            Mock.Of<IProviderSkillCache>(),
            MakeSettings(strict: true));

        // gpt-4o-mini has OpenAi backend but rejects the Skills shell tool.
        var agent = MakeAgent(modelId: Models.OpenAi.Gpt4oMini);
        var skill = MakeUploadableSkill();

        var ex = Assert.Throws<SkillNotSupportedException>(
            () => composer.Apply(agent, [skill], BackendType.OpenAi));

        Assert.Equal(BackendType.OpenAi, ex.Backend);
        Assert.Equal(Models.OpenAi.Gpt4oMini, ex.ModelId);
        Assert.Contains("model does not support Skills API", ex.Reason);
    }

    [Fact]
    public void Strict_UploadableSkill_CacheMiss_Throws()
    {
        var cacheMock = new Mock<IProviderSkillCache>();
        cacheMock
            .Setup(c => c.TryGet(It.IsAny<BackendType>(), It.IsAny<string>(), out It.Ref<ProviderSkillReference?>.IsAny))
            .Returns(new TryGetCallback((BackendType _, string _, out ProviderSkillReference? r) =>
            {
                r = null;
                return false;
            }));

        var composer = new SkillComposer(
            Mock.Of<ILogger<SkillComposer>>(),
            cacheMock.Object,
            MakeSettings(strict: true));

        var agent = MakeAgent(modelId: "gpt-5.5");
        var skill = MakeUploadableSkill();

        var ex = Assert.Throws<SkillNotSupportedException>(
            () => composer.Apply(agent, [skill], BackendType.OpenAi));

        Assert.Contains("cache miss", ex.Reason);
    }

    [Fact]
    public void Strict_CodeDefinedSkill_OnUnsupportedBackend_DoesNotThrow()
    {
        var composer = new SkillComposer(
            Mock.Of<ILogger<SkillComposer>>(),
            Mock.Of<IProviderSkillCache>(),
            MakeSettings(strict: true));

        var agent = MakeAgent(modelId: "gemini-2.5-pro");
        // Skill with C# Execute delegate — fundamentally cannot be uploaded; strict mode must
        // not throw for it (would be unfixable from user code).
        var codeSkill = new AgentSkill
        {
            Name = "web-search",
            Tools =
            [
                new SkillToolDefinition
                {
                    Name = "search",
                    Description = "search",
                    Parameters = "{}",
                    Execute = async _ => "result"
                }
            ]
        };

        composer.Apply(agent, [codeSkill], BackendType.Gemini);

        // No exception; the skill stays out of provider references (no bundle to upload).
        Assert.Empty(agent.ProviderSkillReferences);
    }

    [Fact]
    public void NonStrict_AllUnsupportedCases_FallbackToInline()
    {
        var cacheMock = new Mock<IProviderSkillCache>();
        cacheMock
            .Setup(c => c.TryGet(It.IsAny<BackendType>(), It.IsAny<string>(), out It.Ref<ProviderSkillReference?>.IsAny))
            .Returns(new TryGetCallback((BackendType _, string _, out ProviderSkillReference? r) =>
            {
                r = null;
                return false;
            }));

        var composer = new SkillComposer(
            Mock.Of<ILogger<SkillComposer>>(),
            cacheMock.Object,
            MakeSettings(strict: false));

        // All three unsupported scenarios in one composer call; default behaviour falls back
        // to inline composition without throwing.
        var skillUnsupportedBackend = MakeUploadableSkill("a");
        composer.Apply(MakeAgent(modelId: "gemini-2.5-pro"), [skillUnsupportedBackend], BackendType.Gemini);

        var skillUnsupportedModel = MakeUploadableSkill("b");
        composer.Apply(MakeAgent(modelId: Models.OpenAi.Gpt4oMini), [skillUnsupportedModel], BackendType.OpenAi);

        var skillCacheMiss = MakeUploadableSkill("c");
        composer.Apply(MakeAgent(modelId: "gpt-5.5"), [skillCacheMiss], BackendType.OpenAi);

        // Reaching this line without exception proves non-strict regression is intact.
    }
}
