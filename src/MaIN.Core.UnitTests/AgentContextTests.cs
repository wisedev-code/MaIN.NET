using MaIN.Core.Hub.Contexts;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;
using Moq;

namespace MaIN.Core.UnitTests;

public class AgentContextTests
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly AgentContext _agentContext;

    public AgentContextTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _agentContext = new AgentContext(_mockAgentService.Object);
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
        Assert.NotNull(agent.Context);
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
        // Arrange
        var expectedModel = "gpt-4";

        // Act
        var result = _agentContext.WithModel(expectedModel);

        // Assert
        Assert.Equal(expectedModel, _agentContext.GetAgent().Model);
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
        Assert.Equal(expectedPrompt, _agentContext.GetAgent().Context.Instruction);
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
        Assert.Equal(expectedSteps, _agentContext.GetAgent().Context.Steps);
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
        var agent = new Agent();
        _mockAgentService
            .Setup(s => s.CreateAgent(
                It.IsAny<Agent>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<InferenceParams>()))
            .ReturnsAsync(agent);

        // Act
        var result = await _agentContext.CreateAsync(true, false);

        // Assert
        _mockAgentService.Verify(
            s => s.CreateAgent(
                It.IsAny<Agent>(), 
                It.Is<bool>(f => f == true), 
                It.Is<bool>(r => r == false), 
                It.IsAny<InferenceParams>()),
            Times.Once);
        Assert.Equal(_agentContext, result);
    }

    [Fact]
    public async Task ProcessAsync_WithStringMessage_ShouldReturnChatResult()
    {
        // Arrange
        var message = "Hello, agent!";
        var chat = new Chat { Id = _agentContext.GetAgentId(), Messages = new List<Message>() };
        var chatResult = new ChatResult { Done = true, Model = "test-model" };

        _mockAgentService
            .Setup(s => s.GetChatByAgent(_agentContext.GetAgentId()))
            .ReturnsAsync(chat);

        _mockAgentService
            .Setup(s => s.Process(It.IsAny<Chat>(), _agentContext.GetAgentId(), It.IsAny<bool>()))
            .ReturnsAsync(new Chat { 
                Model = "test-model", 
                Messages = new List<Message> { 
                    new Message { Content = "Response", Role = "Assistant" } 
                } 
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
        var existingAgent = new Agent { Id = existingAgentId, Name = "Existing Agent" };

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
    public async Task FromExisting_ShouldThrowArgumentExceptionWhenAgentNotFound()
    {
        // Arrange
        var nonExistentAgentId = "non-existent-agent-id";

        _mockAgentService
            .Setup(s => s.GetAgentById(nonExistentAgentId))
            .ReturnsAsync((Agent)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            AgentContext.FromExisting(_mockAgentService.Object, nonExistentAgentId));
    }
}