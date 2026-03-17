using MaIN.Domain.Configuration;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Entities.ProviderParams;

public class XaiParams : IProviderInferenceParams
{
    public BackendType Backend => BackendType.Xai;

    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 4096;
    public float TopP { get; init; } = 1.0f;
    public float FrequencyPenalty { get; init; }
    public float PresencePenalty { get; init; }
    public Grammar? Grammar { get; set; }
}
