using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.LLMService;

public sealed class GroqCloudService(
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

    protected override string HttpClientName => ServiceConstants.HttpClients.GroqCloudClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.GroqCloudOpenAiChatCompletions;
    protected override string ModelsUrl => ServiceConstants.ApiUrls.GroqCloudModels;

    protected override string GetApiKey()
    {
        return _settings.GroqCloudKey ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ??
            throw new InvalidOperationException("GroqCloud Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.GroqCloudKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
        {
            throw new InvalidOperationException("GroqCloud Key not configured");
        }
    }

    public override Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Embeddings are not supported by the Groq Cloud model. Document reading requires embedding support.");
    }
}
