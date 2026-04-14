using System.Text.Json;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.IntegrationTests;

[Collection("IntegrationTests")]
public class AnthropicServiceTests : LLMServiceTestBase
{
    private const string ModelId = "claude-sonnet-4-5";

    public AnthropicServiceTests()
    {
        ModelRegistry.RegisterOrReplace(new GenericCloudModel(ModelId, BackendType.Anthropic));
        HttpHandler.ResponseBody = AnthropicResponse("ok");
    }

    [Fact]
    public async Task Should_SetMaxTokens_DefaultTo4096_WhenNotSpecified()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        JsonElement root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(4096, root.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Should_MapMaxTokens_FromAnthropicInferenceParams()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .WithInferenceParams(new AnthropicInferenceParams { MaxTokens = 2048 })
            .CompleteAsync();

        JsonElement root = HttpHandler.LastRequestJson!.RootElement;
        Assert.Equal(2048, root.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Should_ExtractSystemPrompt_ToTopLevelField()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hello")
            .WithSystemPrompt("Be helpful")
            .CompleteAsync();

        JsonElement root = HttpHandler.LastRequestJson!.RootElement;
        Assert.True(root.TryGetProperty("system", out JsonElement systemProp));
        Assert.Equal("Be helpful", systemProp.GetString());
    }

    [Fact]
    public async Task Should_SendImages_AsBase64_WithMediaType()
    {
        const string visionModelId = "claude-sonnet-4-5-vision";
        ModelRegistry.RegisterOrReplace(new GenericCloudVisionModel(visionModelId, BackendType.Anthropic));

        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes

        await AIHub.Chat()
            .WithModel(visionModelId)
            .WithMessage("describe this image", imageBytes)
            .CompleteAsync();

        JsonElement root = HttpHandler.LastRequestJson!.RootElement;
        JsonElement messages = root.GetProperty("messages");
        JsonElement userMessage = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");

        Assert.NotEqual(default, userMessage);
        JsonElement content = userMessage.GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        JsonElement imagePart = content.EnumerateArray()
            .FirstOrDefault(p => p.GetProperty("type").GetString() == "image");
        Assert.NotEqual(default, imagePart);
        Assert.Equal("base64", imagePart.GetProperty("source").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Should_IncludeXApiKeyHeader()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        Assert.True(HttpHandler.LastRequest!.Headers.Contains("x-api-key"));
    }

    [Fact]
    public async Task Should_IncludeAnthropicVersionHeader()
    {
        await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        Assert.True(HttpHandler.LastRequest!.Headers.Contains("anthropic-version"));
    }

    [Fact]
    public async Task Should_ParseContent_FromNonStreamingResponse()
    {
        HttpHandler.ResponseBody = AnthropicResponse("hello");

        ChatResult result = await AIHub.Chat()
            .WithModel(ModelId)
            .WithMessage("hi")
            .CompleteAsync();

        Assert.Equal("hello", result.Message.Content);
    }

    [Fact]
    public async Task Should_UseInputSchema_NotParameters_ForTools()
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

        JsonElement root = HttpHandler.LastRequestJson!.RootElement;
        JsonElement toolsArray = root.GetProperty("tools");
        JsonElement tool = toolsArray[0];

        Assert.True(tool.TryGetProperty("input_schema", out _));
        Assert.False(tool.TryGetProperty("parameters", out _));
    }

    [Fact]
    public async Task Should_ThrowInvalidBackendParamsException_WhenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel(ModelId)
                .WithMessage("hi")
                .WithInferenceParams(new OpenAiInferenceParams())
                .CompleteAsync());
    }
}
