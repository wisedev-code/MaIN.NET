using MaIN.Core.IntegrationTests.Fakes;
using MaIN.Domain.Entities;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Core.IntegrationTests;

public class PipelineTestBase : IntegrationTestBase
{
    protected readonly FakeLLMServiceFactory FakeFactory = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<ILLMServiceFactory>(FakeFactory);

    protected void SetTextResponse(string content) =>
        FakeFactory.Service.Handler = chat => new ChatResult
        {
            Model = chat.ModelId ?? "fake",
            Done = true,
            CreatedAt = DateTime.UtcNow,
            Message = new Message { Role = "assistant", Content = content, Type = MessageType.CloudLLM }
        };
}
