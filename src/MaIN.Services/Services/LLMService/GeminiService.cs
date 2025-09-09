using MaIN.Domain.Configuration;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaIN.Domain.Entities;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.LLMService;

public sealed class GeminiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<GeminiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    private readonly IMemoryService _memoryService = memoryService;
    private readonly IMemoryFactory _memoryFactory = memoryFactory;

    protected override string HttpClientName => ServiceConstants.HttpClients.GeminiClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.GeminiOpenAiChatCompletions;

    public override async Task<string[]> GetCurrentModels()
    {
        ValidateApiKey();

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.GeminiClient);
        client.DefaultRequestHeaders.Add("x-goog-api-key", [GetApiKey()]);

        using var response = await client.GetAsync(ServiceConstants.ApiUrls.GeminiModels);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var modelsResponse = JsonSerializer.Deserialize<GeminiModelsResponse>(responseJson);

        return modelsResponse?.Models?
                   .Where(m => m.Name!.StartsWith("models/gemini", StringComparison.InvariantCultureIgnoreCase))
                   .Select(m => m.Name![7..]) // remove "models/" part => get baseModelId
                   .ToArray()
               ?? [];
    }

    protected override string GetApiKey()
    {
        return _settings.GeminiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            throw new InvalidOperationException("Gemini Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.GeminiKey) &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
        {
            throw new InvalidOperationException("Gemini Key not configured");
        }
    }

    public override async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        if (!chat.Messages.Any())
            return null;

        var kernel = _memoryFactory.CreateMemoryWithGemini(GetApiKey(), chat.MemoryParams);

        await _memoryService.ImportDataToMemory((kernel, null), memoryOptions, cancellationToken);

        var userQuery = chat.Messages.Last().Content;
        if (chat.MemoryParams.Grammar != null)
        {
            var jsonGrammarConverter = new GBNFToJsonConverter();
            var jsonGrammar = jsonGrammarConverter.ConvertToJson(chat.MemoryParams.Grammar);
            userQuery =
                $"{userQuery} | For your next response only, please respond using exactly the following JSON format: \n{jsonGrammar}\n. Do not include any explanations, code blocks, or additional content. After this single JSON response, resume your normal conversational style.";
        }

        var retrievedContext = await kernel.AskAsync(userQuery, cancellationToken: cancellationToken);
        chat.Messages.Last().MarkProcessed();
        await kernel.DeleteIndexAsync(cancellationToken: cancellationToken);
        return CreateChatResult(chat, retrievedContext.Result, []);
    }
}

file class GeminiModelsResponse
{
    [JsonPropertyName("models")] public List<GeminiModel>? Models { get; set; }
}

file class GeminiModel
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}