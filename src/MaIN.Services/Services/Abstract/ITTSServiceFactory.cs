using MaIN.Domain.Configuration;
using MaIN.Services.Services.TTSService;

namespace MaIN.Services.Services.Abstract;

public interface ITTSServiceFactory
{
    ITTSService CreateService(BackendType backendType);
}