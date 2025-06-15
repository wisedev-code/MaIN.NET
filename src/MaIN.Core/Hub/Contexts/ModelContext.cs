using System.Net;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.LLMService.Utils;

namespace MaIN.Core.Hub.Contexts;

public class ModelContext
{
    private readonly MaINSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    private const int DefaultBufferSize = 8192;
    private const int FileStreamBufferSize = 65536;
    private const int ProgressUpdateIntervalSeconds = 1;
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromMinutes(30);

    internal ModelContext(MaINSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public List<Model> GetAll() => KnownModels.All();

    public Model GetModel(string model) => KnownModels.GetModel(model);

    public Model GetEmbeddingModel() => KnownModels.GetEmbeddingModel();

    public bool Exists(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        var model = KnownModels.GetModel(modelName);
        var modelPath = GetModelFilePath(model.FileName);
        return File.Exists(modelPath);
    }

    public async Task<ModelContext> DownloadAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        var model = KnownModels.GetModel(modelName);
        await DownloadModelAsync(model.DownloadUrl!, model.FileName, cancellationToken);
        return this;
    }

    public async Task<ModelContext> DownloadAsync(string model, string url)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        var fileName = $"{model}.gguf";
        await DownloadModelAsync(url, fileName, CancellationToken.None);
        
        var filePath = GetModelFilePath(fileName);
        KnownModels.AddModel(model, filePath);
        return this;
    }

    public ModelContext Download(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        var model = KnownModels.GetModel(modelName);
        DownloadModelSync(model.DownloadUrl!, model.FileName);
        return this;
    }

    public ModelContext Download(string model, string url)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model name cannot be null or empty", nameof(model));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        var fileName = $"{model}.gguf";
        DownloadModelSync(url, fileName);
        
        var filePath = GetModelFilePath(fileName);
        KnownModels.AddModel(model, filePath);
        return this;
    }

    public ModelContext LoadToCache(Model model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var modelsPath = ResolvePath(_settings.ModelsPath);
        ModelLoader.GetOrLoadModel(modelsPath, model.FileName);
        return this;
    }

    public async Task<ModelContext> LoadToCacheAsync(Model model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var modelsPath = ResolvePath(_settings.ModelsPath);
        await ModelLoader.GetOrLoadModelAsync(modelsPath, model.FileName);
        return this;
    }

    private async Task DownloadModelAsync(string url, string fileName, CancellationToken cancellationToken)
    {
        using var httpClient = CreateConfiguredHttpClient();
        var filePath = GetModelFilePath(fileName);
        
        Console.WriteLine($"Starting download of {fileName}...");

        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await DownloadWithProgressAsync(response, filePath, fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download failed: {ex.Message}");
            
            if (!File.Exists(filePath)) throw;
            File.Delete(filePath);
            throw;
        }
    }

    private async Task DownloadWithProgressAsync(HttpResponseMessage response, string filePath, string fileName, CancellationToken cancellationToken)
    {
        var totalBytes = response.Content.Headers.ContentLength;
        var totalBytesRead = 0L;
        var buffer = new byte[DefaultBufferSize];
        var lastProgressUpdate = DateTime.Now;
        var startTime = DateTime.Now;

        if (totalBytes.HasValue)
        {
            Console.WriteLine($"File size: {FormatBytes(totalBytes.Value)}");
        }

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, FileStreamBufferSize);
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        while (true)
        {
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0) break;

            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;

            if (ShouldUpdateProgress(lastProgressUpdate))
            {
                ShowProgress(totalBytesRead, totalBytes, startTime);
                lastProgressUpdate = DateTime.Now;
            }
        }

        ShowFinalProgress(totalBytesRead, totalBytes, startTime, fileName);
    }

    private void DownloadModelSync(string url, string fileName)
    {
        var filePath = GetModelFilePath(fileName);
        
        Console.WriteLine($"Starting download of {fileName}...");

        using var webClient = CreateConfiguredWebClient();
        var startTime = DateTime.Now;
        var lastProgressUpdate = DateTime.Now;
        
        webClient.DownloadProgressChanged += (sender, e) =>
        {
            if (ShouldUpdateProgress(lastProgressUpdate))
            {
                ShowProgress(e.BytesReceived, e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive : null, startTime);
                lastProgressUpdate = DateTime.Now;
            }
        };
        
        webClient.DownloadFileCompleted += (sender, e) =>
        {
            if (e.Error != null)
            {
                Console.WriteLine($"\nDownload failed: {e.Error.Message}");
            }
            else
            {
                var totalTime = DateTime.Now - startTime;
                Console.WriteLine($"\nDownload completed: {fileName}. Time: {totalTime:hh\\:mm\\:ss}");
            }
        };

        try
        {
            webClient.DownloadFile(url, filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download failed: {ex.Message}");

            if (!File.Exists(filePath)) throw;
            File.Delete(filePath); 
            throw;
        }
    }

    private HttpClient CreateConfiguredHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ModelContextDownloadClient);
        httpClient.Timeout = DefaultHttpTimeout;
        return httpClient;
    }

    private static WebClient CreateConfiguredWebClient()
    {
        var webClient = new WebClient();
        webClient.Headers.Add("User-Agent", "YourApp/1.0");
        return webClient;
    }

    private string GetModelFilePath(string fileName) => Path.Combine(ResolvePath(_settings.ModelsPath), fileName);

    private static bool ShouldUpdateProgress(DateTime lastUpdate) => 
        DateTime.Now - lastUpdate > TimeSpan.FromSeconds(ProgressUpdateIntervalSeconds);

    private static void ShowProgress(long totalBytesRead, long? totalBytes, DateTime startTime)
    {
        var elapsed = DateTime.Now - startTime;
        var speed = elapsed.TotalSeconds > 0 ? totalBytesRead / elapsed.TotalSeconds : 0;

        if (totalBytes.HasValue)
        {
            var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
            var eta = speed > 0 ? TimeSpan.FromSeconds((totalBytes.Value - totalBytesRead) / speed) : TimeSpan.Zero;

            Console.Write($"\rProgress: {progressPercentage:F1}% ({FormatBytes(totalBytesRead)}/{FormatBytes(totalBytes.Value)}) " +
                         $"Speed: {FormatBytes((long)speed)}/s ETA: {eta:hh\\:mm\\:ss}");
        }
        else
        {
            Console.Write($"\rDownloaded: {FormatBytes(totalBytesRead)} Speed: {FormatBytes((long)speed)}/s");
        }
    }

    private static void ShowFinalProgress(long totalBytesRead, long? totalBytes, DateTime startTime, string fileName)
    {
        var totalTime = DateTime.Now - startTime;
        var avgSpeed = totalTime.TotalSeconds > 0 ? totalBytesRead / totalTime.TotalSeconds : 0;

        Console.WriteLine($"\nDownload completed: {fileName}. " +
                         $"Total size: {FormatBytes(totalBytesRead)}, " +
                         $"Time: {totalTime:hh\\:mm\\:ss}, " +
                         $"Average speed: {FormatBytes((long)avgSpeed)}/s");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 Bytes";

        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes >= max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            max /= scale;
        }

        return "0 Bytes";
    }

    private string ResolvePath(string? settingsModelsPath) =>
        settingsModelsPath
        ?? Environment.GetEnvironmentVariable("MaIN_ModelsPath")
        ?? throw new InvalidOperationException("Models path not found in settings or environment variables");
}