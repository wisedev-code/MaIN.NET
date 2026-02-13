using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Utils;
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
        string apiKey = _settings.OpenAiKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName) 
            ?? throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var requestBody = new
        {
            model = chat.ModelId,
            prompt = BuildPromptFromChat(chat),
            size = ServiceConstants.Defaults.ImageSize
        };

        using var response = await client.PostAsJsonAsync(ServiceConstants.ApiUrls.OpenAiImageGenerations, requestBody);
        var imageUrl = await ProcessOpenAiResponse(response);
        byte[] imageBytes = await DownloadImageAsync(imageUrl);
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
    
    private async Task<string> ProcessOpenAiResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<OpenAiImageResponse>();
        return responseData?.Data.FirstOrDefault()?.Url 
               ?? throw new InvalidOperationException("No image URL returned from OpenAI");
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