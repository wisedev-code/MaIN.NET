using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Factory;

namespace MaIN.Core.IntegrationTests.Fakes;

public sealed class FakeLLMServiceFactory : ILLMServiceFactory
{
    public FakeLLMService Service { get; } = new();

    public ILLMService CreateService(BackendType backendType) => Service;
}
