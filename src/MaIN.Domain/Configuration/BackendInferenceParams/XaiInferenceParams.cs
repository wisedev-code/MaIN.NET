using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using Grammar = MaIN.Domain.Models.Grammar;

namespace MaIN.Domain.Configuration.BackendInferenceParams;

public class XaiInferenceParams : IBackendInferenceParams
{
    public BackendType Backend => BackendType.Xai;

    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public float? TopP { get; init; }
    public float? FrequencyPenalty { get; init; }
    public float? PresencePenalty { get; init; }
    public Grammar? Grammar { get; set; }
}
