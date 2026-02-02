using System.Diagnostics;
using System.Net;
using MaIN.Core.Hub.Contexts.Interfaces.ModelContext;
using MaIN.Domain.Configuration;
using MaIN.Domain.Exceptions.Models;
using MaIN.Domain.Exceptions.Models.LocalModels;
using MaIN.Domain.Models.Abstract;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.LLMService.Utils;

namespace MaIN.Core.Hub.Contexts;

public sealed class ModelContext : IModelContext
{
    private readonly MaINSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    private const int DefaultBufferSize = 8192;
    private const int FileStreamBufferSize = 65536;
    private const int ProgressUpdateIntervalMilliseconds = 1000;
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromMinutes(30);

    internal ModelContext(MaINSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public IEnumerable<AIModel> GetAll() => ModelRegistry.GetAll();

    public IEnumerable<LocalModel> GetAllLocal() => ModelRegistry.GetAllLocal();

    public AIModel GetModel(string modelId) => ModelRegistry.GetById(modelId);

    public AIModel GetEmbeddingModel() => new Nomic_Embedding();

    public bool Exists(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        var model = ModelRegistry.GetById(modelId);
        if (model is not LocalModel localModel)
        {
            return false; // Cloud models don't have local files
        }
        
        var modelPath = GetModelFilePath(localModel.FileName);
        return File.Exists(modelPath);
    }

    public async Task<IModelContext> DownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        var model = ModelRegistry.GetById(modelId) ?? throw new ModelNotSupportedException(modelId);
        
        if (model is not LocalModel localModel)
        {
            throw new InvalidModelTypeException(nameof(LocalModel));
        }
        
        if (localModel.DownloadUrl is null)
        {
            throw new DownloadUrlNullOrEmptyException();
        }
        
        await DownloadModelAsync(localModel.DownloadUrl.ToString(), localModel.FileName, cancellationToken);
        return this;
    }

    public async Task<IModelContext> DownloadAsync(string modelId, string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DownloadUrlNullOrEmptyException();
        }

        var fileName = $"{modelId}.gguf";
        await DownloadModelAsync(url, fileName, cancellationToken);
        
        // Register the newly downloaded model
        var newModel = new GenericLocalModel(
            FileName: fileName,
            Name: modelId,
            Id: modelId,
            DownloadUrl: new Uri(url)
        );
        ModelRegistry.RegisterOrReplace(newModel);
        
        return this;
    }

    [Obsolete("Use async method instead")]
    public IModelContext Download(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        var model = ModelRegistry.GetById(modelId) ?? throw new ModelNotSupportedException(modelId);
        if (model is not LocalModel localModel)
        {
            throw new MissingModelIdException(nameof(LocalModel));
        }

        if (localModel.DownloadUrl is null)
        {
            throw new DownloadUrlNullOrEmptyException();
        }

        DownloadModelSync(localModel.DownloadUrl.ToString(), localModel.FileName);
        return this;
    }

    [Obsolete("Use async method instead")]
    public IModelContext Download(string modelId, string url)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DownloadUrlNullOrEmptyException();
        }

        var fileName = $"{modelId}.gguf";
        DownloadModelSync(url, fileName);

        // Register the newly downloaded model
        var newModel = new GenericLocalModel(
            FileName: fileName,
            Name: modelId,
            Id: modelId,
            DownloadUrl: new Uri(url)
        );
        ModelRegistry.RegisterOrReplace(newModel);

        return this;
    }

    public IModelContext LoadToCache(LocalModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var modelsPath = ResolvePath(_settings.ModelsPath);
        ModelLoader.GetOrLoadModel(modelsPath, model.FileName);
        return this;
    }

    public async Task<IModelContext> LoadToCacheAsync(LocalModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

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

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            throw;
        }
    }

    private async Task DownloadWithProgressAsync(HttpResponseMessage response, string filePath, string fileName, CancellationToken cancellationToken)
    {
        var totalBytes = response.Content.Headers.ContentLength;
        var totalBytesRead = 0L;
        var buffer = new byte[DefaultBufferSize];
        var progressStopwatch = Stopwatch.StartNew();
        var totalStopwatch = Stopwatch.StartNew();

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

            if (ShouldUpdateProgress(progressStopwatch))
            {
                ShowProgress(totalBytesRead, totalBytes, totalStopwatch);
                progressStopwatch.Restart();
            }
        }

        ShowFinalProgress(totalBytesRead, totalStopwatch, fileName);
    }

    [Obsolete("Use async method instead")]
    private void DownloadModelSync(string url, string fileName)
    {
        var filePath = GetModelFilePath(fileName);
        
        Console.WriteLine($"Starting download of {fileName}...");

        using var webClient = CreateConfiguredWebClient();
        var totalStopwatch = Stopwatch.StartNew();
        var progressStopwatch = Stopwatch.StartNew();
        
        webClient.DownloadProgressChanged += (sender, e) =>
        {
            if (ShouldUpdateProgress(progressStopwatch))
            {
                ShowProgress(e.BytesReceived, e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive : null, totalStopwatch);
                progressStopwatch.Restart();
            }
        };
        
        webClient.DownloadFileCompleted += (sender, e) =>
        {
            totalStopwatch.Stop();
            if (e.Error != null)
            {
                Console.WriteLine($"\nDownload failed: {e.Error.Message}");
            }
            else
            {
                var totalTime = totalStopwatch.Elapsed;
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

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }
    }

    private HttpClient CreateConfiguredHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ModelContextDownloadClient);
        httpClient.Timeout = DefaultHttpTimeout;
        return httpClient;
    }

    [Obsolete("Obsolete")]
    private static WebClient CreateConfiguredWebClient()
    {
        var webClient = new WebClient();
        webClient.Headers.Add("User-Agent", "YourApp/1.0");
        return webClient;
    }

    private string GetModelFilePath(string fileName) => Path.Combine(ResolvePath(_settings.ModelsPath), fileName);

    private static bool ShouldUpdateProgress(Stopwatch progressStopwatch) => 
        progressStopwatch.ElapsedMilliseconds >= ProgressUpdateIntervalMilliseconds;

    private static void ShowProgress(long totalBytesRead, long? totalBytes, Stopwatch totalStopwatch)
    {
        var elapsedSeconds = totalStopwatch.Elapsed.TotalSeconds;
        var speed = elapsedSeconds > 0 ? totalBytesRead / elapsedSeconds : 0;

        if (totalBytes.HasValue)
        {
            var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
            var eta = speed > 0 ? TimeSpan.FromSeconds((totalBytes.Value - totalBytesRead) / speed) : TimeSpan.Zero;

            var (leftBefore, topBefore) = Console.GetCursorPosition();
            Console.Write($"\rProgress: {progressPercentage:F1}% ({FormatBytes(totalBytesRead)}/{FormatBytes(totalBytes.Value)}) " +
                         $"Speed: {FormatBytes((long)speed)}/s ETA: {eta:hh\\:mm\\:ss}");

            var (leftAfter, topAfter) = Console.GetCursorPosition();
            int lengthDifference = leftBefore - leftAfter + (topBefore - topAfter) * Console.WindowWidth;
            while (lengthDifference > 0)
            {
                Console.Write(' ');
                lengthDifference--;
            }
            Console.SetCursorPosition(leftAfter, topAfter);
        }
        else
        {
            Console.Write($"\rDownloaded: {FormatBytes(totalBytesRead)} Speed: {FormatBytes((long)speed)}/s");
        }
    }

    private static void ShowFinalProgress(long totalBytesRead, Stopwatch totalStopwatch, string fileName)
    {
        totalStopwatch.Stop();
        var totalTime = totalStopwatch.Elapsed;
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
        string[] orders = ["GB", "MB", "KB", "Bytes"];
        var max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (var order in orders)
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
        ?? throw new ModelsPathNotFoundException();
}