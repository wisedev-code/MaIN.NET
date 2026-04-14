using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class VertexInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Vertex;

    public string Location { get; init; } = "us-central1";

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public float? TopP { get; init; }
    public string[]? StopSequences { get; init; }
    public Grammar? Grammar { get; set; }
    public Dictionary<string, object>? AdditionalParams { get; init; }
}
