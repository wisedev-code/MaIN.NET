using MaIN.Core.Hub.Contexts;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using Moq;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.UnitTests;

public class ChatContextTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly ChatContext _chatContext;

    public ChatContextTests()
    {
        _mockChatService = new Mock<IChatService>();
        _chatContext = new ChatContext(_mockChatService.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeNewChat()
    {
        // Act
        var chatId = _chatContext.GetChatId();
        
        // Assert
        Assert.NotNull(chatId);
        Assert.NotEmpty(chatId);
    }

    [Fact]
    public void WithMessage_ShouldAddMessage()
    {
        // Act
        _chatContext.WithMessage("Hello, world!");
        var messages = _chatContext.GetChatHistory();
        
        // Assert
        Assert.Single(messages);
        Assert.Equal("Hello, world!", messages[0].Content);
        Assert.Equal("User", messages[0].Role);
    }

    [Fact]
    public void WithSystemPrompt_ShouldInsertSystemMessageAtBeginning()
    {
        // Act
        _chatContext.WithMessage("User message");
        _chatContext.WithSystemPrompt("System prompt");
        var messages = _chatContext.GetChatHistory();
        
        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Equal("System", messages[0].Role);
        Assert.Equal("System prompt", messages[0].Content);
    }

    [Fact]
    public void WithFiles_ShouldAttachFilesToLastMessage()
    {
        // Arrange
        _chatContext.WithMessage("User message");
        var files = new List<FileInfo> { new() { Name = "file.txt", Path = "/path/file.txt", Extension = "txt"} };
        
        // Act
        _chatContext.WithFiles(files);
        
        // Assert
        var lastMessage = _chatContext.GetChatHistory().Last();
        Assert.NotNull(lastMessage);
    }

    [Fact]
    public async Task CompleteAsync_ShouldCallChatService()
    {
        // Arrange
        var chatResult = new ChatResult(){ Model = "test-model", Message = new Message
            {
                Role = "Assistant",
                Content = "test-message",
                Type = MessageType.LocalLLM
            }
        };
        

        _mockChatService.Setup(s => s.Completions(It.IsAny<Chat>(), It.IsAny<bool>(), It.IsAny<bool>(), null))
            .ReturnsAsync(chatResult);
        
        _chatContext.WithMessage("User message");

        // Act
        var result = await _chatContext.CompleteAsync();
        
        // Assert
        _mockChatService.Verify(s => s.Completions(It.IsAny<Chat>(), false, false, null), Times.Once);
        Assert.Equal(chatResult, result);
    }

    [Fact]
    public async Task GetCurrentChat_ShouldCallChatService()
    {
        // Arrange
        var chat = new Chat { Id = _chatContext.GetChatId(), ModelId = "default", Name = "test"};
        _mockChatService.Setup(s => s.GetById(chat.Id)).ReturnsAsync(chat);
        
        // Act
        var result = await _chatContext.GetCurrentChat();
        
        // Assert
        Assert.Equal(chat, result);
    }
}