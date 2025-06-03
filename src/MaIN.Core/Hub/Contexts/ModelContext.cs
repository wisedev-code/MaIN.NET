
using System.Net;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Utils;

namespace MaIN.Core.Hub.Contexts;

public class ModelContext
{
    private readonly MaINSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    internal ModelContext(MaINSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }
    
    public List<Model> GetAll()
    {
        return KnownModels.All();
    }
    
    public Model GetModel(string model) 
        => KnownModels.GetModel(model);

    public Model GetEmbeddingModel() 
        => KnownModels.GetEmbeddingModel();
    
    public bool Exists(string modelName)
    {
        var model = KnownModels.GetModel(modelName);
        var path = ResolvePath(_settings.ModelsPath);
        var pathToModel = Path.Combine(path, model.FileName);
        return File.Exists(pathToModel);
    }

    public async Task<ModelContext> DownloadAsync(string modelName)
    {
        using var httpClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ModelContextDownloadClient);
        var model = KnownModels.GetModel(modelName);
        using var response = await httpClient.GetAsync(model.DownloadUrl);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(Path.Combine(ResolvePath(_settings.ModelsPath), model.FileName), FileMode.Create);
        await response.Content.CopyToAsync(fileStream);
        return this;
    }

    public ModelContext Download(string modelName)
    {
        var model = KnownModels.GetModel(modelName);
    
        using var webClient = new WebClient();
        webClient.Headers.Add("User-Agent", "YourApp/1.0");
        webClient.DownloadFile(model.DownloadUrl, Path.Combine(ResolvePath(_settings.ModelsPath),model.FileName));
        return this;
    }
    
    public async Task<ModelContext> DownloadAsync(string model, string url)
    {
        using var httpClient = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ModelContextDownloadClient);
        using var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var path = Path.Combine(ResolvePath(_settings.ModelsPath), $"{model}.gguf");
        await using var fileStream = new FileStream(path, FileMode.Create);
        await response.Content.CopyToAsync(fileStream);
        KnownModels.AddModel(model, path);
        return this;
    }
    
    public ModelContext Download(string model, string url)
    {
        using var webClient = new WebClient();
        webClient.Headers.Add("User-Agent", "YourApp/1.0");
        var path = Path.Combine(ResolvePath(_settings.ModelsPath), $"{model}.gguf");
        webClient.DownloadFile(url, path);
        KnownModels.AddModel(model, path);
        return this;
    }

    public ModelContext LoadToCache(Model model)
    {
        ModelLoader.GetOrLoadModel(ResolvePath(_settings.ModelsPath), model.FileName);
        return this;
    }
    
    public async Task<ModelContext> LoadToCacheAsync(Model model)
    {
        await ModelLoader.GetOrLoadModelAsync(ResolvePath(_settings.ModelsPath), model.FileName);
        return this;
    }

    private string ResolvePath(string? settingsModelsPath) =>
        settingsModelsPath 
        ?? Environment.GetEnvironmentVariable("MaIN_ModelsPath") 
        ?? throw new Exception("Models path not found");
}