using MaIN.Core.Hub.Contexts;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.Abstract;
using Moq;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Exceptions.Flows;

namespace MaIN.Core.UnitTests;

public class FlowContextTests
{
    private readonly Mock<IAgentFlowService> _mockFlowService;
    private readonly Mock<IAgentService> _mockAgentService;
    private FlowContext _flowContext;

    public FlowContextTests()
    {
        _mockFlowService = new Mock<IAgentFlowService>();
        _mockAgentService = new Mock<IAgentService>();
        _flowContext = new FlowContext(_mockFlowService.Object, _mockAgentService.Object);
    }
    
    [Fact]
    public async Task WithId_ShouldSetFlowId()
    {
        // Arrange
        var expectedId = "test-flow-id";

        // Act
        var result = _flowContext.WithId(expectedId);
        
        // Setup mock to return flow with the set ID
        _mockFlowService
            .Setup(s => s.GetFlowById(expectedId))
            .ReturnsAsync(new AgentFlow { Id = expectedId, Name = It.IsAny<string>() });

        var flow = await _flowContext.GetCurrentFlow();

        // Assert
        Assert.Equal(expectedId, flow.Id);
        Assert.Equal(result, _flowContext);
    }

    [Fact]
    public async Task WithName_ShouldSetFlowName()
    {
        // Arrange
        var expectedName = "Test Flow";

        // Act
        var result = _flowContext.WithName(expectedName);
        
        // Setup mock to return flow with the set name
        _mockFlowService
            .Setup(s => s.GetFlowById(It.IsAny<string>()))
            .ReturnsAsync(new AgentFlow { 
                Id = It.IsAny<string>(), 
                Name = expectedName 
            });

        var flow = await _flowContext.GetCurrentFlow();

        // Assert
        Assert.Equal(expectedName, flow.Name);
        Assert.Equal(result, _flowContext);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallFlowService()
    {
        // Arrange
        var expectedFlow = new AgentFlow { Id = "new-flow-id", Name = It.IsAny<string>() };
        _mockFlowService
            .Setup(s => s.CreateFlow(It.IsAny<AgentFlow>()))
            .ReturnsAsync(expectedFlow);

        // Act
        var result = await _flowContext.CreateAsync();

        // Assert
        _mockFlowService.Verify(s => s.CreateFlow(It.IsAny<AgentFlow>()), Times.Once);
        Assert.Equal(expectedFlow, result);
    }

    [Fact]
    public async Task ProcessAsync_WithStringMessage_ShouldReturnChatResult()
    {
        // Arrange
        var firstAgent = new Agent { Id = "first-agent", Order = 0, CurrentBehaviour = It.IsAny<string>(), Context = new AgentData()};
        _flowContext.AddAgent(firstAgent);

        var message = "Hello, flow!";
        var chat = new Chat { Id = firstAgent.Id, Messages = new List<Message>(), ModelId = "default", Name = "test"};

        _mockAgentService
            .Setup(s => s.GetChatByAgent(firstAgent.Id))
            .ReturnsAsync(chat);

        _mockAgentService
            .Setup(s => s.Process(It.IsAny<Chat>(), firstAgent.Id, It.IsAny<Knowledge>(), It.IsAny<bool>(), null, null))
            .ReturnsAsync(new Chat { 
                ModelId = "test-model", 
                Name = "test",
                Messages = new List<Message> { 
                    new() { Content = "Response", Role = "Assistant", Type = MessageType.LocalLLM} 
                } 
            });

        // Act
        var result = await _flowContext.ProcessAsync(message);

        // Assert
        Assert.True(result.Done);
        Assert.Equal("test-model", result.Model);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task Delete_ShouldCallFlowServiceDelete()
    {
        // Arrange
        var flowId = "test-flow-id";
        _flowContext.WithId(flowId);

        _mockFlowService
            .Setup(s => s.DeleteFlow(flowId))
            .Returns(Task.CompletedTask);

        // Act
        await _flowContext.Delete();

        // Assert
        _mockFlowService.Verify(s => s.DeleteFlow(flowId), Times.Once);
    }

    [Fact]
    public async Task GetAllFlows_ShouldReturnListOfFlows()
    {
        // Arrange
        var expectedFlows = new List<AgentFlow>
        {
            new() { Id = "flow1", Name = "Flow 1" },
            new() { Id = "flow2", Name = "Flow 2" }
        };

        _mockFlowService
            .Setup(s => s.GetAllFlows())
            .ReturnsAsync(expectedFlows);

        // Act
        var result = await _flowContext.GetAllFlows();

        // Assert
        Assert.Equal(expectedFlows, result);
    }

    [Fact]
    public async Task FromExisting_ShouldCreateFlowContextFromExistingFlow()
    {
        // Arrange
        var existingFlowId = "existing-flow-id";
        var existingFlow = new AgentFlow 
        { 
            Id = existingFlowId, 
            Name = "Existing Flow",
            Agents =
            [
                new Agent
                {
                    Id = "agent1",
                    CurrentBehaviour = It.IsAny<string>(),
                    Context = new AgentData()
                }
            ]
        };

        _mockFlowService
            .Setup(s => s.GetFlowById(existingFlowId))
            .ReturnsAsync(existingFlow);

        // Act
        var result = await _flowContext.FromExisting(existingFlowId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FromExisting_ShouldThrowArgumentExceptionWhenFlowNotFound()
    {
        // Arrange
        var nonExistentFlowId = "non-existent-flow-id";
        _mockFlowService
            .Setup(s => s.GetFlowById(nonExistentFlowId))
            .ReturnsAsync((AgentFlow)null!);

        // Act & Assert
        await Assert.ThrowsAsync<FlowNotFoundException>(() => _flowContext.FromExisting(nonExistentFlowId));
    }
}