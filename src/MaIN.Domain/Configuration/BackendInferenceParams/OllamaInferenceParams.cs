using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class OllamaInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Ollama;

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? TopK { get; init; }
    public float? TopP { get; init; }
    public int? NumCtx { get; init; }
    public int? NumGpu { get; init; }
    public Grammar? Grammar { get; set; }
    public Dictionary<string, object>? AdditionalParams { get; init; }
}
