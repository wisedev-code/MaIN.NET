using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Domain.Models;
using MaIN.Services.Services;
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
}
