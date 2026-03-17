using MaIN.Domain.Configuration;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Entities.ProviderParams;

public class GeminiParams : IProviderInferenceParams
{
    public BackendType Backend => BackendType.Gemini;

    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 4096;
    public int TopK { get; init; } = 40;
    public float TopP { get; init; } = 0.95f;
    public string[]? StopSequences { get; init; }
    public Grammar? Grammar { get; set; }
}
