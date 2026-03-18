using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public interface IBackendInferenceParams
{
    BackendType Backend { get; }
}
