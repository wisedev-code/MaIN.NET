using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Auth;
using MaIN.Services.Services.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MaIN.Services.Services.ImageGenServices;

internal class VertexImageGenService(IHttpClientFactory httpClientFactory, MaINSettings settings) : IImageGenService
{
    private const string DefaultModel = "imagen-4.0-generate-001";
    private const string DefaultLocation = "us-central1";

    public async Task<ChatResult?> Send(Chat chat)
    {
        var auth = settings.GoogleServiceAccountAuth
                   ?? throw new InvalidOperationException("Vertex AI service account is not configured.");

        var location = chat.BackendParams is VertexInferenceParams vp
            ? vp.Location
            : DefaultLocation;

        using var tokenProvider = new GoogleServiceAccountTokenProvider(auth);
        var httpClient = httpClientFactory.CreateClient(ServiceConstants.HttpClients.VertexClient);
        var accessToken = await tokenProvider.GetAccessTokenAsync(httpClient);

        var model = ExtractModelName(chat.ModelId);
        var endpoint = $"https://{location}-aiplatform.googleapis.com/v1/projects/{auth.ProjectId}/locations/{location}/publishers/google/models/{model}:predict";

        var requestBody = new
        {
            instances = new[]
            {
                new { prompt = BuildPromptFromChat(chat) }
            },
            parameters = new
            {
                sampleCount = 1,
                aspectRatio = "1:1"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(requestBody);

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Vertex AI Imagen request failed ({response.StatusCode}): {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<ImagenResponse>();
        var base64Image = result?.Predictions?.FirstOrDefault()?.BytesBase64Encoded;

        if (string.IsNullOrEmpty(base64Image))
            throw new InvalidOperationException("No image returned from Vertex AI Imagen.");

        var imageBytes = Convert.FromBase64String(base64Image);

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
            Model = chat.ModelId ?? $"google/{DefaultModel}",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildPromptFromChat(Chat chat)
    {
        return chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");
    }

    /// <summary>
    /// Strips the "google/" publisher prefix if present (Vertex predict endpoint doesn't use it).
    /// </summary>
    private static string ExtractModelName(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            return DefaultModel;

        return modelId.StartsWith("google/", StringComparison.OrdinalIgnoreCase)
            ? modelId["google/".Length..]
            : modelId;
    }
}

file class ImagenResponse
{
    [JsonPropertyName("predictions")]
    public ImagenPrediction[]? Predictions { get; set; }
}

file class ImagenPrediction
{
    [JsonPropertyName("bytesBase64Encoded")]
    public string? BytesBase64Encoded { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}
