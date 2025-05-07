using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Models;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaIN.Services.Services.LLMService;

public class GeminiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<GeminiService>? logger = null)
    : ILLMService
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> SessionCache = new();

    public Task<ChatResult?> Send(Chat chat, ChatRequestOptions requestOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ChatResult?> AskMemory(Chat chat, ChatMemoryOptions memoryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string[]> GetCurrentModels()
    {
        ValidateApiKey();

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.GeminiClient);
        client.DefaultRequestHeaders.Add("x-goog-api-key", [GetApiKey()]);

        using var response = await client.GetAsync(ServiceConstants.ApiUrls.GeminiModels);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var modelsResponse = JsonSerializer.Deserialize<GeminiModelsResponse>(responseJson);

        return (modelsResponse?.Models?
                    .Where(m => m.Name!.StartsWith("models/gemini", StringComparison.InvariantCultureIgnoreCase))
                    .Where(id => id != null)
                    .Select(m => m.Name[7..]) // remove "models/" part => get baseModelId
                    .ToArray()
                ?? [])!;
    }

    public Task CleanSessionCache(string id)
    {
        throw new NotImplementedException();
    }

    private string GetApiKey()
    {
        return _settings.GeminiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            throw new InvalidOperationException("Gemini Key not configured");
    }

    private void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.GeminiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
        {
            throw new InvalidOperationException("Gemini Key not configured");
        }
    }
}

file class GeminiModelsResponse
{
    [JsonPropertyName("models")]
    public List<GeminiModel>? Models { get; set; }
}

file class GeminiModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
