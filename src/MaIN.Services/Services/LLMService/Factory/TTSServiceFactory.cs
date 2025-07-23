using MaIN.Domain.Configuration;
using MaIN.Services.Services.TTSService;

namespace MaIN.Services.Services.LLMService.Factory;

public interface ITTSServiceFactory
{
    ITTSService CreateService(BackendType backendType);
}

public class TTSServiceFactory(IServiceProvider serviceProvider) : ITTSServiceFactory
{
    public ITTSService CreateService(BackendType backendType)
    {
        return backendType switch
        {
            BackendType.Self => new TTSService.TTSService(),
            _ => throw new ArgumentOutOfRangeException(nameof(backendType))
        };
    }
}