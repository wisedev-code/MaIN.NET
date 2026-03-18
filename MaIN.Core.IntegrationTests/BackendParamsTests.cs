using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Concrete;

namespace MaIN.Core.IntegrationTests;

public class BackendParamsTests : IntegrationTestBase
{
    private const string TestQuestion = "What is 2+2? Answer with just the number.";

    [SkippableFact]
    public async Task OpenAi_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.OpenAi)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<Gpt4oMini>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new OpenAiInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task Anthropic_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Anthropic)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<ClaudeSonnet4>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new AnthropicInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task Gemini_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Gemini)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<Gemini2_0Flash>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new GeminiInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task DeepSeek_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.DeepSeek)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<DeepSeekReasoner>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new DeepSeekInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task GroqCloud_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.GroqCloud)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<Llama3_1_8bInstant>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new GroqCloudInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task Xai_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Xai)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<Grok3Beta>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new XaiInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task Self_Should_RespondWithParams()
    {
        Skip.If(!File.Exists("C:/Models/gemma2-2b.gguf"), "Local model not found at C:/Models/gemma2-2b.gguf");

        var result = await AIHub.Chat()
            .WithModel<Gemma2_2b>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new LocalInferenceParams
            {
                Temperature = 0.3f,
                ContextSize = 8192,
                MaxTokens = 100,
                TopK = 40,
                TopP = 0.9f
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task LocalOllama_Should_RespondWithParams()
    {
        SkipIfOllamaNotRunning();

        var result = await AIHub.Chat()
            .WithModel<OllamaGemma3_4b>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new OllamaInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopK = 40,
                TopP = 0.9f,
                NumCtx = 2048
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    [SkippableFact]
    public async Task ClaudOllama_Should_RespondWithParams()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Ollama)?.ApiKeyEnvName!);

        var result = await AIHub.Chat()
            .WithModel<OllamaGemma3_4b>()
            .WithMessage(TestQuestion)
            .WithInferenceParams(new OllamaInferenceParams
            {
                Temperature = 0.3f,
                MaxTokens = 100,
                TopK = 40,
                TopP = 0.9f,
                NumCtx = 2048
            })
            .CompleteAsync();

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("4", result.Message.Content);
    }

    // --- Params mismatch validation (no API key required) ---

    [Fact]
    public async Task Self_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<Gemma2_2b>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new OpenAiInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task OpenAi_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<Gpt4oMini>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new DeepSeekInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task Anthropic_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<ClaudeSonnet4>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new OpenAiInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task Gemini_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<Gemini2_0Flash>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new AnthropicInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task DeepSeek_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<DeepSeekReasoner>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new GeminiInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task GroqCloud_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<Llama3_1_8bInstant>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new OpenAiInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task Xai_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<Grok3Beta>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new AnthropicInferenceParams())
                .CompleteAsync());
    }

    [Fact]
    public async Task Ollama_Should_ThrowWhenGivenWrongParams()
    {
        await Assert.ThrowsAsync<InvalidBackendParamsException>(() =>
            AIHub.Chat()
                .WithModel<OllamaGemma3_4b>()
                .WithMessage(TestQuestion)
                .WithInferenceParams(new DeepSeekInferenceParams())
                .CompleteAsync());
    }

    private static void SkipIfMissingKey(string envName)
    {
        Skip.If(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envName)),
            $"{envName} environment variable not set");
    }

    private static void SkipIfOllamaNotRunning()
    {
        Skip.If(!Helpers.NetworkHelper.PingHost("127.0.0.1", 11434, 3),
            "Ollama is not running on localhost:11434");
    }
}
