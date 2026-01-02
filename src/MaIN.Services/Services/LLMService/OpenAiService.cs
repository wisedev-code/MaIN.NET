using MaIN.Domain.Configuration;
using MaIN.Domain.Exceptions;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;

namespace MaIN.Services.Services.LLMService;

public sealed class OpenAiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
      
    protected override string GetApiKey()
    {
        return _settings.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            throw new APIKeyNotConfiguredException("OpenAi");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.OpenAiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            throw new APIKeyNotConfiguredException("OpenAi");
        }
    }

    public override async Task<string[]> GetCurrentModels()
    {
        var allModels = await base.GetCurrentModels();

        return allModels
            .Where(id => id.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
    }
}
