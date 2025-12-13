using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MaIN.Services.Services.ImageGenServices;

public class XaiImageGenService(
    IHttpClientFactory httpClientFactory,
    MaINSettings settings)
    : IImageGenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<ChatResult?> Send(Chat chat)
    {
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.XaiClient);
        string apiKey = _settings.XaiKey ?? Environment.GetEnvironmentVariable("XAI_API_KEY") ??
            throw new InvalidOperationException("xAI Key not configured");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var requestBody = new
        {
            model = string.IsNullOrWhiteSpace(chat.Model) ? Models.GROK_IMAGE : chat.Model,
            prompt = BuildPromptFromChat(chat),
            n = 1,
            response_format = "b64_json" //or "url"
        };

        using var response = await client.PostAsJsonAsync(ServiceConstants.ApiUrls.XaiImageGenerations, requestBody);
        var imageBytes = await ProcessXaiResponse(response);
        return CreateChatResult(imageBytes);
    }

    private static string BuildPromptFromChat(Chat chat)
    {
        return chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");
    }

    private async Task<byte[]> ProcessXaiResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<XaiImageResponse>(new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        var first = responseData?.Data.FirstOrDefault()
                    ?? throw new InvalidOperationException("No image data returned from xAI");

        if (!string.IsNullOrEmpty(first.B64Json))
        {
            return Convert.FromBase64String(first.B64Json);
        }

        if (!string.IsNullOrEmpty(first.Url))
        {
            return await DownloadImageAsync(first.Url);
        }

        throw new InvalidOperationException("No image content returned from xAI");
    }

    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {
        var imageClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ImageDownloadClient);

        using var imageResponse = await imageClient.GetAsync(imageUrl);
        imageResponse.EnsureSuccessStatusCode();

        return await imageResponse.Content.ReadAsByteArrayAsync();
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
                Image = imageBytes,
                Type = MessageType.Image
            },
            Model = Models.GROK_IMAGE,
            CreatedAt = DateTime.UtcNow
        };
    }

    private struct Models
    {
        public const string GROK_IMAGE = "grok-2-image";
    }
}


file class XaiImageResponse
{
    public XaiImageData[] Data { get; set; } = [];
}

file class XaiImageData
{
    public string? Url { get; set; }
    public string? B64Json { get; set; }
    public string? RevisedPrompt { get; set; }
}