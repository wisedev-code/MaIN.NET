using MaIN.Domain.Configuration;

namespace MaIN.Core.UnitTests;

public class BackendCapabilitiesTests
{
    [Theory]
    [InlineData(BackendType.OpenAi, true)]
    [InlineData(BackendType.Anthropic, true)]
    [InlineData(BackendType.Self, false)]
    [InlineData(BackendType.Gemini, false)]
    [InlineData(BackendType.DeepSeek, false)]
    [InlineData(BackendType.GroqCloud, false)]
    [InlineData(BackendType.Xai, false)]
    [InlineData(BackendType.Ollama, false)]
    [InlineData(BackendType.Vertex, false)]
    public void HasNativeSkillsApi_OnlyOpenAiAndAnthropic(BackendType backend, bool expected)
    {
        Assert.Equal(expected, backend.HasNativeSkillsApi());
    }

    [Theory]
    // OpenAI: skills land on gpt-5.5+ only. gpt-4*, gpt-5-nano, gpt-3.5 reject the shell tool.
    [InlineData(BackendType.OpenAi, "gpt-5.5", true)]
    [InlineData(BackendType.OpenAi, "gpt-5.5-turbo", true)]
    [InlineData(BackendType.OpenAi, "gpt-6", true)]
    [InlineData(BackendType.OpenAi, "gpt-5", false)]
    [InlineData(BackendType.OpenAi, "gpt-5-nano", false)]
    [InlineData(BackendType.OpenAi, "gpt-4o", false)]
    [InlineData(BackendType.OpenAi, "gpt-4o-mini", false)]
    [InlineData(BackendType.OpenAi, "gpt-4.1-mini", false)]
    [InlineData(BackendType.OpenAi, "gpt-3.5-turbo", false)]
    [InlineData(BackendType.OpenAi, "garbage", false)]
    [InlineData(BackendType.OpenAi, null, false)]
    [InlineData(BackendType.OpenAi, "", false)]
    // Anthropic: skills require claude-opus-4+ (sonnet/haiku reject).
    [InlineData(BackendType.Anthropic, "claude-opus-4-7", true)]
    [InlineData(BackendType.Anthropic, "claude-opus-5", true)]
    [InlineData(BackendType.Anthropic, "claude-3-5-sonnet", false)]
    [InlineData(BackendType.Anthropic, "claude-haiku-3", false)]
    // Non-skills backends reject regardless of model.
    [InlineData(BackendType.Gemini, "gpt-5.5", false)]
    [InlineData(BackendType.Self, "anything", false)]
    public void SupportsSkillsApi(BackendType backend, string? modelId, bool expected)
    {
        Assert.Equal(expected, backend.SupportsSkillsApi(modelId));
    }
}
