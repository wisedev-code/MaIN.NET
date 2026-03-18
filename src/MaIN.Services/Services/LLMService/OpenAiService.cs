using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Domain.Configuration.BackendInferenceParams;

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

    protected override Type ExpectedParamsType => typeof(OpenAiInferenceParams);

    protected override string GetApiKey()
    {
        return _settings.OpenAiKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName) ??
            throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);
    }

    protected override string GetApiName() => LLMApiRegistry.OpenAi.ApiName;

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.OpenAiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName)))
        {
            throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);
        }
    }

    protected override void ApplyBackendParams(Dictionary<string, object> requestBody, Chat chat)
    {
        if (chat.BackendParams is not OpenAiInferenceParams p) return;
        if (p.MaxTokens.HasValue) requestBody["max_completion_tokens"] = p.MaxTokens.Value;
        if (p.Temperature.HasValue) requestBody["temperature"] = p.Temperature.Value;
        if (p.TopP.HasValue) requestBody["top_p"] = p.TopP.Value;
        if (p.FrequencyPenalty.HasValue) requestBody["frequency_penalty"] = p.FrequencyPenalty.Value;
        if (p.PresencePenalty.HasValue) requestBody["presence_penalty"] = p.PresencePenalty.Value;
        if (p.ResponseFormat != null) requestBody["response_format"] = new { type = p.ResponseFormat };
    }

    public override async Task<string[]> GetCurrentModels()
    {
        var allModels = await base.GetCurrentModels();

        return allModels
            .Where(id => id.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
    }
}
