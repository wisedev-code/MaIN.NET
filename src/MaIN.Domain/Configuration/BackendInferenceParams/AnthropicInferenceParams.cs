using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class AnthropicInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Anthropic;

    public float Temperature { get; init; } = 1.0f;
    public int MaxTokens { get; init; } = 4096;
    public int TopK { get; init; } = -1;
    public float TopP { get; init; } = 1.0f;
    public Grammar? Grammar { get; set; }
}
