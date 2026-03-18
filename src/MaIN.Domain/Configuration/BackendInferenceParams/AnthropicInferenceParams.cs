using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class AnthropicInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Anthropic;

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? TopK { get; init; }
    public float? TopP { get; init; }
    public Grammar? Grammar { get; set; }
}
