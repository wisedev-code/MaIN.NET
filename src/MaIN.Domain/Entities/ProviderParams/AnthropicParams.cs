using MaIN.Domain.Configuration;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Entities.ProviderParams;

public class AnthropicParams : IProviderInferenceParams
{
    public BackendType Backend => BackendType.Anthropic;

    public float Temperature { get; init; } = 1.0f;
    public int MaxTokens { get; init; } = 4096;
    public int TopK { get; init; } = -1;
    public float TopP { get; init; } = 1.0f;
    public Grammar? Grammar { get; set; }
}
