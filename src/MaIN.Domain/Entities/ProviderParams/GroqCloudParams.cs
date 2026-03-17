using MaIN.Domain.Configuration;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Entities.ProviderParams;

public class GroqCloudParams : IProviderInferenceParams
{
    public BackendType Backend => BackendType.GroqCloud;

    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 4096;
    public float TopP { get; init; } = 1.0f;
    public float FrequencyPenalty { get; init; }
    public string? ResponseFormat { get; init; }
    public Grammar? Grammar { get; set; }
}
