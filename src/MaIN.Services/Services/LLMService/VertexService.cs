using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Auth;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.LLMService;

public sealed class VertexService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<VertexService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger), ILLMService
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private GoogleServiceAccountTokenProvider? _tokenProvider;
    private string _location = "us-central1";

    protected override string HttpClientName => ServiceConstants.HttpClients.VertexClient;

    protected override string ChatCompletionsUrl
    {
        get
        {
            var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
            return $"https://{_location}-aiplatform.googleapis.com/v1beta1/projects/{auth.ProjectId}/locations/{_location}/endpoints/openapi/chat/completions";
        }
    }

    protected override string ModelsUrl
    {
        get
        {
            var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
            return $"https://{_location}-aiplatform.googleapis.com/v1beta1/projects/{auth.ProjectId}/locations/{_location}/endpoints/openapi/models";
        }
    }

    protected override Type ExpectedParamsType => typeof(VertexInferenceParams);

    protected override string GetApiKey()
    {
        var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.VertexAuth is not configured.");

        _tokenProvider ??= new GoogleServiceAccountTokenProvider(auth);

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        return _tokenProvider.GetAccessTokenAsync(httpClient).GetAwaiter().GetResult();
    }

    protected override string GetApiName() => LLMApiRegistry.Vertex.ApiName;

    protected override void ValidateApiKey()
    {
        var auth = _settings.GoogleServiceAccountAuth;
        if (auth == null)
            throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
        if (string.IsNullOrEmpty(auth.ProjectId))
            throw new InvalidOperationException("GoogleServiceAccountConfig.ProjectId is required.");
        if (string.IsNullOrEmpty(auth.ClientEmail))
            throw new InvalidOperationException("GoogleServiceAccountConfig.ClientEmail is required.");
        if (string.IsNullOrEmpty(auth.PrivateKey))
            throw new InvalidOperationException("GoogleServiceAccountConfig.PrivateKey is required.");
    }

    protected override void ApplyBackendParams(Dictionary<string, object> requestBody, Chat chat)
    {
        if (chat.BackendParams is not VertexInferenceParams p) return;
        if (p.Temperature.HasValue) requestBody["temperature"] = p.Temperature.Value;
        if (p.MaxTokens.HasValue) requestBody["max_tokens"] = p.MaxTokens.Value;
        if (p.TopP.HasValue) requestBody["top_p"] = p.TopP.Value;
        if (p.StopSequences is { Length: > 0 }) requestBody["stop"] = p.StopSequences;
    }

    public new async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ExtractLocation(chat);
        return await base.Send(chat, options, cancellationToken);
    }

    public new async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        ExtractLocation(chat);
        return await base.AskMemory(chat, memoryOptions, requestOptions, cancellationToken);
    }

    private void ExtractLocation(Chat chat)
    {
        if (chat.BackendParams is VertexInferenceParams vp)
            _location = vp.Location;
    }
}
