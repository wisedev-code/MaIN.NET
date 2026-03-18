using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class GeminiInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Gemini;

    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 4096;
    public float TopP { get; init; } = 0.95f;
    public string[]? StopSequences { get; init; }
    public Grammar? Grammar { get; set; }
}
