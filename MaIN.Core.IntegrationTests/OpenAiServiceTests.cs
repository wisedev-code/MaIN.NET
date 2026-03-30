using System.Text.Json;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Core.IntegrationTests;

[Collection("IntegrationTests")]
public class OpenAiServiceTests : LLMServiceTestBase
{
    private const string ModelId = "gpt-4o-mini";

    public OpenAiServiceTests()
    {
        ModelRegistry.RegisterOrReplace(new GenericCloudModel(ModelId, BackendType.OpenAi));
        HttpHandler.ResponseBody = OpenAiResponse("ok");
    }

    [Fact]
    public async Task Should_SendModelId_InRequestBody()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(ModelId, root.GetProperty("model").GetString());
    }

    [Fact]
    public async Task Should_SendUserMessage_InMessagesArray()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hello world")
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        var messages = root.GetProperty("messages");
        var userMessage = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");

        Assert.NotEqual(default, userMessage);
        Assert.Equal("hello world", userMessage.GetProperty("content").GetString());
    }

    [Fact]
    public async Task Should_SendStreamFalse_ForNonStreaming()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.False(root.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task Should_SendStreamTrue_ForStreaming()
    {
        HttpHandler.ResponseBody = OpenAiStreamResponse("hello");

        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync(interactive: true);

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.True(root.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task Should_MapTemperature_FromOpenAiInferenceParams()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .WithInferenceParams(new OpenAiInferenceParams { Temperature = 0.7f })
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(0.7f, root.GetProperty("temperature").GetSingle());
    }

    [Fact]
    public async Task Should_MapMaxTokens_FromOpenAiInferenceParams()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .WithInferenceParams(new OpenAiInferenceParams { MaxTokens = 512 })
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(512, root.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Should_MapTopP_FromOpenAiInferenceParams()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .WithInferenceParams(new OpenAiInferenceParams { TopP = 0.9f })
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(0.9f, root.GetProperty("top_p").GetSingle());
    }

    [Fact]
    public async Task Should_ParseContent_FromNonStreamingResponse()
    {
        HttpHandler.ResponseBody = OpenAiResponse("hello");

        var result = await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        Assert.Equal("hello", result.Message.Content);
    }

    [Fact]
    public async Task Should_SendAuthorizationHeader_WithBearerToken()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        Assert.NotNull(HttpHandler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", HttpHandler.LastRequest!.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Should_IncludeVisionContent_WhenModelIsVision()
    {
        const string visionModelId = "gpt-4o-vision";
        ModelRegistry.RegisterOrReplace(new GenericCloudVisionModel(visionModelId, BackendType.OpenAi));

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes

        await AIHub.Chat()
            .WithModel(visionModelId)
            .WithMessage("describe this image", imageBytes)
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        var messages = root.GetProperty("messages");
        var userMessage = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");

        Assert.NotEqual(default, userMessage);
        var content = userMessage.GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.Contains(content.EnumerateArray()
, part => part.GetProperty("type").GetString() == "image_url");
    }

    [Fact]
    public async Task Should_IncludeToolsArray_WhenToolsConfigured()
    {
        var tools = new ToolsConfiguration
        {
            Tools =
            [
                new ToolDefinition
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get current weather",
                        Parameters = new { type = "object", properties = new { } }
                    },
                    Execute = _ => Task.FromResult("sunny")
                }
            ]
        };

        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("what's the weather?")
            .WithTools(tools)
            .CompleteAsync();

        var root = HttpHandler.LastRequestJson!.RootElement;
        var toolsArray = root.GetProperty("tools");
        Assert.Equal(JsonValueKind.Array, toolsArray.ValueKind);
        Assert.Equal("get_weather",
            toolsArray[0].GetProperty("function").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Should_ThrowInvalidBackendParamsException_WhenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel(ModelId)
                .WithMessage("hi")
                .WithInferenceParams(new AnthropicInferenceParams())
                .CompleteAsync());
    }
}
