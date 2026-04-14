using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using ModelIds = MaIN.Domain.Models.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MaIN.Services.Services.ImageGenServices;

public class OpenAiImageGenService(
    IHttpClientFactory httpClientFactory,
    MaINSettings settings)
    : IImageGenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    
    public async Task<ChatResult?> Send(Chat chat)
    {
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);
        string apiKey = _settings.OpenAiKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName)
            ?? throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);

        var model = string.IsNullOrEmpty(chat.ModelId) ? ModelIds.OpenAi.DallE3 : chat.ModelId;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var requestBody = new
        {
            model,
            prompt = BuildPromptFromChat(chat),
            size = ServiceConstants.Defaults.ImageSize
        };

        using var response = await client.PostAsJsonAsync(ServiceConstants.ApiUrls.OpenAiImageGenerations, requestBody);

        byte[] imageBytes = await ProcessOpenAiResponse(response);
        return CreateChatResult(imageBytes, model);
    }
    
    private static string BuildPromptFromChat(Chat chat)
    {
        return chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");
    }
    
    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {
        var imageClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ImageDownloadClient);
        
        using var imageResponse = await imageClient.GetAsync(imageUrl);
        imageResponse.EnsureSuccessStatusCode();
        
        return await imageResponse.Content.ReadAsByteArrayAsync();
    }

    private async Task<byte[]> ProcessOpenAiResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<OpenAiImageResponse>();

        var imageData = responseData?.Data.FirstOrDefault()
                        ?? throw new InvalidOperationException("No image data returned from OpenAI");

        if (!string.IsNullOrEmpty(imageData.B64Json))
            return Convert.FromBase64String(imageData.B64Json);

        if (!string.IsNullOrEmpty(imageData.Url))
            return await DownloadImageAsync(imageData.Url);

        throw new InvalidOperationException("No image URL or base64 data returned from OpenAI");
    }
    
    private static ChatResult CreateChatResult(byte[] imageBytes, string model)
    {
        return new ChatResult
        {
            Done = true,
            Message = new Message
            {
                Content = ServiceConstants.Messages.GeneratedImageContent,
                Role = ServiceConstants.Roles.Assistant,
                Image = imageBytes,
                Type = MessageType.Image
            },
            Model = model,
            CreatedAt = DateTime.UtcNow
        };
    }
}

file class OpenAiImageResponse
{
    public ImageData[] Data { get; set; } = [];
}

file class ImageData
{
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("b64_json")]
    public string B64Json { get; set; } = string.Empty;
}