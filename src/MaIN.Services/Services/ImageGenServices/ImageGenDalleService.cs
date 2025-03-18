using System.Net.Http.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services.ImageGenServices;

public class OpenAiImageGenService(
    IHttpClientFactory httpClientFactory,
    MaINSettings options) : IImageGenService
{
  public async Task<ChatResult?> Send(Chat chat)
{
    using var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromMinutes(5);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    
    var prompt = (chat.Messages
        .Select((msg, index) => index == 0 ? msg.Content
            : $"&& {msg.Content}")
        .Aggregate((current, next) => $"{current} {next}"));
    
    var requestBody = new
    {
        model = Models.DALLE,
        prompt = prompt,
        size = "1024x1024"
    };

    var response = await client.PostAsJsonAsync("https://api.openai.com/v1/images/generations", requestBody);

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to create image for chat {chat.Id} with message " +
                            $"{chat.Messages.Last().Content}, status code {response.StatusCode}");
    }

    var responseData = await response.Content.ReadFromJsonAsync<OpenAiImageResponse>();
    if (responseData == null || responseData.Data.Length == 0)
    {
        throw new Exception("No image URL returned from OpenAI.");
    }

    using var imageClient = new HttpClient();
    using var imageResponse = await imageClient.GetAsync(responseData.Data[0].Url);

    if (!imageResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to download image, status code {imageResponse.StatusCode}");
    }

    var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

    var result = new ChatResult()
    {
        Done = true,
        Message = new MessageDto()
        {
            Content = "Generated Image:",
            Role = "Assistant",
            Images = imageBytes
        },
        Model = Models.DALLE,
        CreatedAt = DateTime.Now
    };

    return result;
}

    
    public struct Models
    {
        public const string DALLE = "dall-e-3";
    }
}

public class OpenAiImageResponse
{
    public ImageData[] Data { get; set; } = [];
}

public class ImageData
{
    public string Url { get; set; } = string.Empty;
}
