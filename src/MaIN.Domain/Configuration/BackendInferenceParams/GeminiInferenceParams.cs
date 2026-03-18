using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class GeminiInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Gemini;

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public float? TopP { get; init; }
    public string[]? StopSequences { get; init; }
    public Grammar? Grammar { get; set; }
    public Dictionary<string, object>? AdditionalParams { get; init; }
}
