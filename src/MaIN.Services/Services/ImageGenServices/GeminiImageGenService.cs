using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MaIN.Services.Services.ImageGenServices;

internal class GeminiImageGenService(IHttpClientFactory httpClientFactory, MaINSettings settings) : IImageGenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<ChatResult?> Send(Chat chat)
    {
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.GeminiClient);
        string apiKey = _settings.GeminiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("Gemini API key is not configured");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var requestBody = new
        {
            model = Models.IMAGEN_GENERATE,
            prompt = BuildPromptFromChat(chat),
            response_format = "b64_json", // necessary for gemini api
            size = ServiceConstants.Defaults.ImageSize,
        };

        using var response = await client.PostAsJsonAsync(ServiceConstants.ApiUrls.GeminiImageGenerations, requestBody);
        var imageBytes = await ProcessGeminiResponse(response);
        return CreateChatResult(imageBytes);
    }

    private static string BuildPromptFromChat(Chat chat)
    {
        return chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");
    }

    private async Task<byte[]> ProcessGeminiResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<GeminiImageResponse>();

        var base64Image = responseData?.Data.FirstOrDefault()?.Base64Image;
        if (base64Image is null || string.IsNullOrWhiteSpace(base64Image))
        {
            throw new InvalidOperationException("No image returned from Gemini");
        }

        return Convert.FromBase64String(base64Image);
    }

    private static ChatResult CreateChatResult(byte[] imageBytes)
    {
        return new ChatResult
        {
            Done = true,
            Message = new Message
            {
                Content = ServiceConstants.Messages.GeneratedImageContent,
                Role = ServiceConstants.Roles.Assistant,
                Image = imageBytes
            },
            Model = Models.IMAGEN_GENERATE,
            CreatedAt = DateTime.UtcNow
        };
    }

    private struct Models
    {
        public const string IMAGEN_GENERATE = "imagen-3.0-generate-002";
    }
}

file class GeminiImageResponse
{
    public ImageData[] Data { get; set; } = [];
}

file class ImageData
{
    [JsonPropertyName("b64_json")]
    public string Base64Image { get; set; } = string.Empty;
}