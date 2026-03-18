using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class OllamaInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Ollama;

    public float Temperature { get; init; } = 0.8f;
    public int MaxTokens { get; init; } = 4096;
    public int TopK { get; init; } = 40;
    public float TopP { get; init; } = 0.9f;
    public int NumCtx { get; init; } = 2048;
    public int NumGpu { get; init; } = 30;
    public Grammar? Grammar { get; set; }
}
