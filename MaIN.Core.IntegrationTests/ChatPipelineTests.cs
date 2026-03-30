using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.IntegrationTests;

[Collection("IntegrationTests")]
public class ChatPipelineTests : PipelineTestBase
{
    private const string TestModelId = "pipeline-test-model";

    public ChatPipelineTests()
    {
        ModelRegistry.RegisterOrReplace(new GenericCloudModel(TestModelId, BackendType.OpenAi));
        SetTextResponse("default response");
    }

    [Fact]
    public async Task Should_ReturnDone_OnSimpleCompletion()
    {
        var result = await AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessage("Hello")
            .CompleteAsync();

        Assert.True(result.Done);
    }

    [Fact]
    public async Task Should_ReturnConfiguredContent_WhenHandlerSet()
    {
        SetTextResponse("custom content");

        var result = await AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessage("Hello")
            .CompleteAsync();

        Assert.Equal("custom content", result.Message.Content);
    }

    [Fact]
    public async Task Should_PersistAssistantMessage_AfterCompletion()
    {
        SetTextResponse("assistant reply");

        var context = AIHub.Chat().WithModel(TestModelId);
        await context
            .WithMessage("Hello")
            .CompleteAsync();

        var chatId = context.GetChatId();
        var existing = await AIHub.Chat().FromExisting(chatId);
        var history = existing.GetChatHistory();

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Should_AccumulateMessages_AcrossMultipleTurns()
    {
        SetTextResponse("reply");

        var context = AIHub.Chat().WithModel(TestModelId);

        await context.WithMessage("Turn 1").CompleteAsync();
        await context.WithMessage("Turn 2").CompleteAsync();

        var history = context.GetChatHistory();
        Assert.Equal(4, history.Count);
    }

    [Fact]
    public async Task Should_SendUserMessageToHandler_WithCorrectRole()
    {
        Chat? captured = null;
        FakeFactory.Service.Handler = chat =>
        {
            captured = chat;
            return new ChatResult
            {
                Model = chat.ModelId,
                Done = true,
                CreatedAt = DateTime.UtcNow,
                Message = new Message { Role = "assistant", Content = "ok", Type = MessageType.CloudLLM }
            };
        };

        await AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessage("Hello from user")
            .CompleteAsync();

        Assert.NotNull(captured);
        Assert.Contains(captured!.Messages, m => m.Role == "User");
    }

    [Fact]
    public async Task Should_ApplySystemPrompt_AsFirstMessage()
    {
        Chat? captured = null;
        FakeFactory.Service.Handler = chat =>
        {
            captured = chat;
            return new ChatResult
            {
                Model = chat.ModelId,
                Done = true,
                CreatedAt = DateTime.UtcNow,
                Message = new Message { Role = "assistant", Content = "ok", Type = MessageType.CloudLLM }
            };
        };

        await AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessage("User message")
            .WithSystemPrompt("Be concise")
            .CompleteAsync();

        Assert.NotNull(captured);
        Assert.Equal("System", captured!.Messages[0].Role);
        Assert.Equal("Be concise", captured!.Messages[0].Content);
    }

    [Fact]
    public async Task Should_ThrowEmptyChatException_WhenNoMessageAdded()
    {
        var context = AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessages([]);

        await Assert.ThrowsAsync<EmptyChatException>(() => context.CompleteAsync());
    }

    [Fact]
    public async Task Should_UseLastModel_WhenSetTwice()
    {
        const string secondModel = "pipeline-test-model-2";
        ModelRegistry.RegisterOrReplace(new GenericCloudModel(secondModel, BackendType.OpenAi));

        Chat? captured = null;
        FakeFactory.Service.Handler = chat =>
        {
            captured = chat;
            return new ChatResult
            {
                Model = chat.ModelId,
                Done = true,
                CreatedAt = DateTime.UtcNow,
                Message = new Message { Role = "assistant", Content = "ok", Type = MessageType.CloudLLM }
            };
        };

        var entry = AIHub.Chat();
        entry.WithModel(TestModelId);
        await entry
            .WithModel(secondModel)
            .WithMessage("Hello")
            .CompleteAsync();

        Assert.Equal(secondModel, captured!.ModelId);
    }

    [Fact]
    public async Task Should_SetBackendParams_WhenInferenceParamsProvided()
    {
        Chat? captured = null;
        FakeFactory.Service.Handler = chat =>
        {
            captured = chat;
            return new ChatResult
            {
                Model = chat.ModelId,
                Done = true,
                CreatedAt = DateTime.UtcNow,
                Message = new Message { Role = "assistant", Content = "ok", Type = MessageType.CloudLLM }
            };
        };

        await AIHub.Chat()
            .WithModel(TestModelId)
            .WithMessage("Hello")
            .WithInferenceParams(new OpenAiInferenceParams { Temperature = 0.42f })
            .CompleteAsync();

        Assert.NotNull(captured);
        var openAiParams = Assert.IsType<OpenAiInferenceParams>(captured!.BackendParams);
        Assert.Equal(0.42f, openAiParams.Temperature);
    }
}
