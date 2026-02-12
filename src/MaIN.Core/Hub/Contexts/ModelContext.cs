using AsyncKeyedLock;
using MaIN.Core.Hub.Contexts.Interfaces.ModelContext;
using MaIN.Domain.Configuration;
using MaIN.Domain.Exceptions.Models;
using MaIN.Domain.Exceptions.Models.LocalModels;
using MaIN.Domain.Models.Abstract;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.LLMService.Utils;
using System.Diagnostics;

namespace MaIN.Core.Hub.Contexts;

public sealed class ModelContext : IModelContext
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _defaultModelsPath;

    private readonly AsyncKeyedLocker<string> _downloadLocks = new();

    private const int DefaultBufferSize = 8192;
    private const int FileStreamBufferSize = 65536;
    private const int ProgressUpdateIntervalMilliseconds = 1000;
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromMinutes(30);

    internal ModelContext(MaINSettings settings, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _defaultModelsPath = Environment.GetEnvironmentVariable("MaIN_ModelsPath") ?? settings.ModelsPath;
    }

    public IEnumerable<AIModel> GetAll() => ModelRegistry.GetAll();

    public IEnumerable<LocalModel> GetAllLocal() => ModelRegistry.GetAllLocal();

    public AIModel GetModel(string modelId) => ModelRegistry.GetById(modelId);

    public AIModel GetEmbeddingModel() => new Nomic_Embedding();

    public bool IsDownloaded(string modelId)
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

        return localModel.IsDownloaded(_defaultModelsPath);
    }

    public async Task<IModelContext> EnsureDownloadedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        var model = ModelRegistry.GetById(modelId);

        if (model is not LocalModel localModel || localModel.IsDownloaded(_defaultModelsPath))
        {
            return this;
        }

        using (await _downloadLocks.LockAsync(modelId, cancellationToken))
        {
            // Double-check
            if (!localModel.IsDownloaded(_defaultModelsPath))
            {
                await DownloadModelAsync(localModel, cancellationToken);
            }
        }

        return this;
    }

    public async Task<IModelContext> EnsureDownloadedAsync<TModel>(CancellationToken cancellationToken = default) where TModel : LocalModel, new()
    {
        var model = new TModel();
        return await EnsureDownloadedAsync(model.Id, cancellationToken);
    }

    public async Task<IModelContext> DownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new MissingModelIdException(nameof(modelId));
        }

        var model = ModelRegistry.GetById(modelId);

        if (model is not LocalModel localModel)
        {
            throw new InvalidModelTypeException(nameof(LocalModel));
        }

        using (await _downloadLocks.LockAsync(modelId, cancellationToken))
        {
            await DownloadModelAsync(localModel, cancellationToken);
        }

        return this;
    }

    public IModelContext LoadToCache(LocalModel model)
    {
        var path = model.CustomPath ?? _defaultModelsPath;
        if (string.IsNullOrEmpty(path))
        {
            throw new ModelPathNullOrEmptyException();
        }

        ModelLoader.GetOrLoadModel(path, model.FileName);
        return this;
    }

    public async Task<IModelContext> LoadToCacheAsync(LocalModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        await ModelLoader.GetOrLoadModelAsync(GetModelFilePath(model), model.FileName);
        return this;
    }

    private async Task DownloadModelAsync(LocalModel localModel, CancellationToken cancellationToken)
    {
        using var httpClient = CreateConfiguredHttpClient();
        var filePath = GetModelFilePath(localModel);

        if (localModel.DownloadUrl is null)
        {
            throw new DownloadUrlNullOrEmptyException();
        }

        Console.WriteLine($"Starting download of {localModel.FileName}...");

        try
        {
            using var response = await httpClient.GetAsync(localModel.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await DownloadWithProgressAsync(response, filePath, localModel.FileName, cancellationToken);
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

    private static async Task DownloadWithProgressAsync(HttpResponseMessage response, string filePath, string fileName, CancellationToken cancellationToken)
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
            var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            if (ShouldUpdateProgress(progressStopwatch))
            {
                ShowProgress(totalBytesRead, totalBytes, totalStopwatch);
                progressStopwatch.Restart();
            }
        }

        ShowFinalProgress(totalBytesRead, totalStopwatch, fileName);
    }

    private HttpClient CreateConfiguredHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ModelContextDownloadClient);
        httpClient.Timeout = DefaultHttpTimeout;
        return httpClient;
    }

    private string GetModelFilePath(LocalModel localModel) =>
        localModel.GetFullPath(_defaultModelsPath);

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
            int lengthDifference = leftBefore - leftAfter + ((topBefore - topAfter) * Console.WindowWidth);
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
        if (bytes == 0)
        {
            return "0 Bytes";
        }

        const int scale = 1024;
        string[] orders = ["GB", "MB", "KB", "Bytes"];
        var max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (var order in orders)
        {
            if (bytes >= max)
            {
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            }

            max /= scale;
        }

        return "0 Bytes";
    }
}
