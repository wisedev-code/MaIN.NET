using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class GroqCloudInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.GroqCloud;

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public float? TopP { get; init; }
    public float? FrequencyPenalty { get; init; }
    public string? ResponseFormat { get; init; }
    public Grammar? Grammar { get; set; }
    public Dictionary<string, object>? AdditionalParams { get; init; }
}
