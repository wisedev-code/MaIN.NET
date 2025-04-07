using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

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
        string apiKey = _settings.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OpenAI API key is not configured");
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        string prompt = BuildPromptFromChat(chat);
        var requestBody = new
        {
            model = Models.DALLE,
            prompt = prompt,
            size = ServiceConstants.Defaults.ImageSize
        };

        using var response = await client.PostAsJsonAsync(ServiceConstants.ApiUrls.OpenAiImageGenerations, requestBody);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<OpenAiImageResponse>();
        if (responseData?.Data == null || responseData.Data.Length == 0 || string.IsNullOrEmpty(responseData.Data[0].Url))
        {
            throw new InvalidOperationException("No image URL returned from OpenAI");
        }

        byte[] imageBytes = await DownloadImageAsync(responseData.Data[0].Url);
        return CreateChatResult(imageBytes);
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
    
    private static ChatResult CreateChatResult(byte[] imageBytes)
    {
        return new ChatResult
        {
            Done = true,
            Message = new Message
            {
                Content = ServiceConstants.Messages.GeneratedImageContent,
                Role = ServiceConstants.Messages.AssistantRole,
                Images = imageBytes
            },
            Model = Models.DALLE,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    private struct Models
    {
        public const string DALLE = "dall-e-3";
    }
}


file class OpenAiImageResponse
{
    public ImageData[] Data { get; set; } = [];
}

file class ImageData
{
    public string Url { get; set; } = string.Empty;
}