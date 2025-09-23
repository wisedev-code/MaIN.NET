using MaIN.Domain.Configuration;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.LLMService;

public sealed class XaiService(
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

    protected override string HttpClientName => ServiceConstants.HttpClients.XaiClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.XaiOpenAiChatCompletions;
    protected override string ModelsUrl => ServiceConstants.ApiUrls.XaiModels;

    protected override string GetApiKey()
    {
        return _settings.XaiKey ?? Environment.GetEnvironmentVariable("XAI_API_KEY") ??
            throw new InvalidOperationException("xAI Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.XaiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XAI_API_KEY")))
        {
            throw new InvalidOperationException("xAI Key not configured");
        }
    }

    public override Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(); // todo
    }
}