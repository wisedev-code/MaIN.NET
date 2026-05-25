using MaIN.Core.Hub.Contexts;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using Moq;

namespace MaIN.Core.UnitTests;

public class AgentContextTests
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<ISkillRegistry> _mockSkillRegistry;
    private readonly Mock<ISkillComposer> _mockSkillComposer;
    private readonly AgentContext _agentContext;
    private readonly string _testModelId = "test-model";

    public AgentContextTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockSkillRegistry = new Mock<ISkillRegistry>();
        _mockSkillComposer = new Mock<ISkillComposer>();
        _agentContext = new AgentContext(_mockAgentService.Object, _mockSkillRegistry.Object, _mockSkillComposer.Object);
        var testModel = new GenericLocalModel(_testModelId);
        ModelRegistry.RegisterOrReplace(testModel);
    }

    [Fact]
    public void Constructor_ShouldInitializeNewAgent()
    {
        // Assert
        var agentId = _agentContext.GetAgentId();
        var agent = _agentContext.GetAgent();

        Assert.NotNull(agentId);
        Assert.NotEmpty(agentId);
        Assert.NotNull(agent);
        Assert.NotNull(agent.Config);
        Assert.NotNull(agent.Behaviours);
        Assert.Equal("Agent created by MaIN", agent.Description);
    }

    [Fact]
    public void WithId_ShouldSetAgentId()
    {
        // Arrange
        var expectedId = "test-agent-id";

        // Act
        var result = _agentContext.WithId(expectedId);

        // Assert
        Assert.Equal(expectedId, _agentContext.GetAgentId());
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public void WithName_ShouldSetAgentName()
    {
        // Arrange
        var expectedName = "Test Agent";

        // Act
        var result = _agentContext.WithName(expectedName);

        // Assert
        Assert.Equal(expectedName, _agentContext.GetAgent().Name);
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public void WithModel_ShouldSetAgentModel()
    {
        // Act
        var result = _agentContext.WithModel(_testModelId);

        // Assert
        Assert.Equal(_testModelId, _agentContext.GetAgent().Model);
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public void WithInitialPrompt_ShouldSetInstruction()
    {
        // Arrange
        var expectedPrompt = "You are a helpful assistant";

        // Act
        var result = _agentContext.WithInitialPrompt(expectedPrompt);

        // Assert
        Assert.Equal(expectedPrompt, _agentContext.GetAgent().Config.Instruction);
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public void WithSteps_ShouldSetAgentSteps()
    {
        // Arrange
        var expectedSteps = new List<string> { "ANALYZE", "RESPOND" };

        // Act
        var result = _agentContext.WithSteps(expectedSteps);

        // Assert
        Assert.Equal(expectedSteps, _agentContext.GetAgent().Config.Steps);
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public void WithBehaviour_ShouldAddBehaviourAndSetCurrent()
    {
        // Arrange
        var behaviourName = "Friendly";
        var behaviourInstruction = "Be helpful and friendly";

        // Act
        var result = _agentContext.WithBehaviour(behaviourName, behaviourInstruction);

        // Assert
        var agent = _agentContext.GetAgent();
        Assert.Contains(behaviourName, agent.Behaviours.Keys);
        Assert.Equal(behaviourInstruction, agent.Behaviours[behaviourName]);
        Assert.Equal(behaviourName, agent.CurrentBehaviour);
        Assert.Equal(result, _agentContext);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallAgentServiceCreateAgent()
    {
        // Arrange
        var agent = new Agent()
        {
            Id = Guid.NewGuid().ToString(),
            CurrentBehaviour = "Default",
            Config = new AgentConfig()
        };
        _mockAgentService
            .Setup(s => s.CreateAgent(
                It.IsAny<Agent>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IBackendInferenceParams>(),
                It.IsAny<MemoryParams>(),
                It.IsAny<bool>()))
            .ReturnsAsync(agent);

        // Act
        var result = await _agentContext.CreateAsync(true, false);

        // Assert
        _mockAgentService.Verify(
            s => s.CreateAgent(
                It.IsAny<Agent>(),
                It.Is<bool>(f => f == true),
                It.Is<bool>(r => r == false),
                It.IsAny<IBackendInferenceParams>(),
                It.IsAny<MemoryParams>(),
                It.IsAny<bool>()),
            Times.Once);
        Assert.Equal(_agentContext, result);
    }

    [Fact]
    public async Task ProcessAsync_WithStringMessage_ShouldReturnChatResult()
    {
        // Arrange
        var message = "Hello, agent!";
        var chat = new Chat
        {
            Id = _agentContext.GetAgentId(),
            Messages = [],
            Name = "test",
            ModelId = _testModelId
        };
        var chatResult = new ChatResult
        {
            Done = true,
            Model = "test-model",
            Message = new Message
            {
                Role = "Assistant",
                Content = "Response",
                Type = MessageType.LocalLLM
            }
        };

        _mockAgentService
            .Setup(s => s.GetChatByAgent(_agentContext.GetAgentId()))
            .ReturnsAsync(chat);

        _mockAgentService
            .Setup(s => s.Process(
                It.IsAny<Chat>(),
                _agentContext.GetAgentId(),
                It.IsAny<Knowledge>(),
                It.IsAny<bool>(),
                null,
                null))
            .ReturnsAsync(new Chat
            {
                ModelId = "test-model",
                Name = "test",
                Messages = [
                    new Message { Content = "Response", Role = "Assistant", Type = MessageType.LocalLLM}
                ]
            });

        // Act
        var result = await _agentContext.ProcessAsync(message);

        // Assert
        Assert.True(result.Done);
        Assert.Equal("test-model", result.Model);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task FromExisting_ShouldCreateContextFromExistingAgent()
    {
        // Arrange
        var existingAgentId = "existing-agent-id";
        var existingAgent = new Agent
        {
            Id = existingAgentId,
            Name = "Existing Agent",
            CurrentBehaviour = "Default",
            Config = new AgentConfig()
        };

        _mockAgentService
            .Setup(s => s.GetAgentById(existingAgentId))
            .ReturnsAsync(existingAgent);

        // Act
        var result = await AgentContext.FromExisting(_mockAgentService.Object, existingAgentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingAgentId, result.GetAgentId());
    }

    [Fact]
    public async Task WithAllSkills_SkipsReplacePlacementSkills()
    {
        var composable = new AgentSkill
        {
            Name = "code-review",
            Steps = ["ANSWER"],
            StepPlacement = SkillStepPlacement.Before,
            Tags = [],
            Priority = 30
        };
        var replaceSkill = new AgentSkill
        {
            Name = "funfact-writer",
            Steps = ["MCP"],
            StepPlacement = SkillStepPlacement.Replace,
            Tags = [],
            Priority = 10
        };

        _mockSkillRegistry
            .Setup(r => r.GetAllExcludingBuiltIn())
            .Returns(new List<AgentSkill> { composable, replaceSkill });

        IReadOnlyList<AgentSkill>? composed = null;
        _mockSkillComposer
            .Setup(c => c.Apply(It.IsAny<Agent>(), It.IsAny<IReadOnlyList<AgentSkill>>(), It.IsAny<BackendType?>(), It.IsAny<Knowledge?>()))
            .Callback<Agent, IReadOnlyList<AgentSkill>, BackendType?, Knowledge?>((_, skills, _, _) => composed = skills);

        _mockAgentService
            .Setup(s => s.CreateAgent(
                It.IsAny<Agent>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IBackendInferenceParams>(),
                It.IsAny<MemoryParams>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new Agent { Id = "x", CurrentBehaviour = "Default", Config = new AgentConfig() });

        await _agentContext.WithAllSkills().CreateAsync();

        Assert.NotNull(composed);
        Assert.Single(composed!);
        Assert.Equal("code-review", composed![0].Name);
    }

    [Fact]
    public async Task WithAllSkills_CalledTwice_DoesNotDuplicate()
    {
        var skill = new AgentSkill
        {
            Name = "code-review",
            Steps = ["ANSWER"],
            StepPlacement = SkillStepPlacement.Before,
            Tags = [],
            Priority = 30
        };

        _mockSkillRegistry
            .Setup(r => r.GetAllExcludingBuiltIn())
            .Returns(new List<AgentSkill> { skill });

        IReadOnlyList<AgentSkill>? composed = null;
        _mockSkillComposer
            .Setup(c => c.Apply(It.IsAny<Agent>(), It.IsAny<IReadOnlyList<AgentSkill>>(), It.IsAny<BackendType?>(), It.IsAny<Knowledge?>()))
            .Callback<Agent, IReadOnlyList<AgentSkill>, BackendType?, Knowledge?>((_, skills, _, _) => composed = skills);

        _mockAgentService
            .Setup(s => s.CreateAgent(
                It.IsAny<Agent>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IBackendInferenceParams>(),
                It.IsAny<MemoryParams>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new Agent { Id = "x", CurrentBehaviour = "Default", Config = new AgentConfig() });

        await _agentContext
            .WithAllSkills()
            .WithAllSkills()
            .CreateAsync();

        Assert.NotNull(composed);
        Assert.Single(composed!);
    }

    [Fact]
    public async Task WithAllSkills_PopulatesAgentSkillsList()
    {
        var folderSkill = new AgentSkill
        {
            Name = "folder-skill",
            Steps = ["ANSWER"],
            StepPlacement = SkillStepPlacement.Before,
            Tags = [],
            Priority = 30
        };

        _mockSkillRegistry
            .Setup(r => r.GetAllExcludingBuiltIn())
            .Returns(new List<AgentSkill> { folderSkill });

        _mockAgentService
            .Setup(s => s.CreateAgent(
                It.IsAny<Agent>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IBackendInferenceParams>(),
                It.IsAny<MemoryParams>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new Agent { Id = "x", CurrentBehaviour = "Default", Config = new AgentConfig() });

        await _agentContext.WithAllSkills().CreateAsync();

        Assert.Contains("folder-skill", _agentContext.GetAgent().Skills);
    }

    [Fact]
    public async Task FromExisting_ShouldThrowArgumentExceptionWhenAgentNotFound()
    {
        // Arrange
        var nonExistentAgentId = "non-existent-agent-id";

        _mockAgentService
            .Setup(s => s.GetAgentById(nonExistentAgentId))
            .ReturnsAsync((Agent)null!);

        // Act & Assert
        await Assert.ThrowsAsync<AgentNotFoundException>(() =>
            AgentContext.FromExisting(_mockAgentService.Object, nonExistentAgentId));
    }
}
