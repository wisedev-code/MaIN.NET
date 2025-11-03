using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Examples.SimpleConsole;

namespace MaIN.NET.Examples.TogetherAI;

public static class ImageTools
{
    private const string TogetherApiUrl = "https://api.together.xyz/v1/images/generations";
    private const string ImgbbApiUrl = "https://api.imgbb.com/1/upload";
    private const string Model = "black-forest-labs/FLUX.1-schnell";
    private static readonly HttpClient HttpClient = new();
    private static string? TogetherApiKey => "<>";
    private static string? ImgbbApiKey => "<>";

    public static async Task<object> GenerateImage(GenerateImageArgs args)
    {
        try
        {
            if (string.IsNullOrEmpty(TogetherApiKey))
                return new { error = "Set TOGETHER_API_KEY environment variable" };

            if (string.IsNullOrEmpty(ImgbbApiKey))
                return new { error = "Set IMGBB_API_KEY environment variable" };

            // Generate image with Together AI
            var requestBody = new
            {
                model = Model,
                prompt = args.Prompt,
                width = 1024,
                height = 768,
                steps = 4,
                n = 1,
                response_format = "b64_json"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, TogetherApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TogetherApiKey);
            request.Content = content;

            var response = await HttpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new { error = $"Together API error: {response.StatusCode}", details = responseContent };

            var result = JsonSerializer.Deserialize<TogetherImageResponse>(responseContent);
            if (result?.Data == null || result.Data.Length == 0)
                return new { error = "No image generated" };

            var imageBase64 = result.Data[0].B64Json;

            // Upload to imgbb
            var imgbbUrl = await UploadToImgbb(imageBase64);
            if (imgbbUrl == null)
                return new { error = "Failed to upload to imgbb" };

            return new
            {
                success = true,
                url = imgbbUrl
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    public static async Task<object> EditImage(EditImageArgs args)
    {
        try
        {
            if (string.IsNullOrEmpty(TogetherApiKey))
                return new { error = "Set TOGETHER_API_KEY environment variable" };

            if (string.IsNullOrEmpty(ImgbbApiKey))
                return new { error = "Set IMGBB_API_KEY environment variable" };

            if (string.IsNullOrEmpty(args.ImageUrl))
                return new { error = "Provide ImageUrl" };

            string imageBase64;
            try
            {
                var imageBytes = await HttpClient.GetByteArrayAsync(args.ImageUrl);
                imageBase64 = Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to download image: {ex.Message}" };
            }

            var requestBody = new
            {
                model = Model,
                prompt = args.Prompt,
                image = imageBase64, 
                width = 1024,
                height = 768,
                steps = 4,
                n = 1,
                response_format = "b64_json"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, TogetherApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TogetherApiKey);
            request.Content = content;

            var response = await HttpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new { error = $"Together API error: {response.StatusCode}", details = responseContent };

            var result = JsonSerializer.Deserialize<TogetherImageResponse>(responseContent);
            if (result?.Data == null || result.Data.Length == 0)
                return new { error = "No edited image returned" };

            var editedImageBase64 = result.Data[0].B64Json;

            var imgbbUrl = await UploadToImgbb(editedImageBase64);
            if (imgbbUrl == null)
                return new { error = "Failed to upload to imgbb" };

            return new
            {
                success = true,
                url = imgbbUrl
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    private static async Task<string?> UploadToImgbb(string base64Image)
    {
        try
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(ImgbbApiKey!), "key");
            formData.Add(new StringContent(base64Image), "image");

            var response = await HttpClient.PostAsync(ImgbbApiUrl, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            var result = JsonSerializer.Deserialize<ImgbbResponse>(responseContent);
            return result?.Data?.Url;
        }
        catch
        {
            return null;
        }
    }
}

public class TogetherImageResponse
{
    [JsonPropertyName("data")] public TogetherImageData[]? Data { get; set; }
}

public class TogetherImageData
{
    [JsonPropertyName("b64_json")] public string B64Json { get; set; } = string.Empty;
}

public class ImgbbResponse
{
    [JsonPropertyName("data")] public ImgbbData? Data { get; set; }
}

public class ImgbbData
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}