using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public interface IProviderInferenceParams
{
    BackendType Backend { get; }
}
