using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public interface IBackendInferenceParams
{
    BackendType Backend { get; }
    Dictionary<string, object>? AdditionalParams { get; }
}
