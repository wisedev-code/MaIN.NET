using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Constants;

namespace MaIN.Services.Services.LLMService;

public sealed class DeepSeekService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    protected override string HttpClientName => ServiceConstants.HttpClients.DeepSeekClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.DeepSeekOpenAiChatCompletions;

    protected override string GetApiKey()
    {
        return _settings.DeepSeekKey ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ??
            throw new InvalidOperationException("DeepSeek Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.DeepSeekKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
        {
            throw new InvalidOperationException("DeepSeek Key not configured");
        }
    }
}
