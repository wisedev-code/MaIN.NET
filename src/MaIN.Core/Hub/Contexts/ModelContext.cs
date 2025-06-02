
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;

namespace MaIN.Core.Hub.Contexts;

public class ModelContext
{
    private readonly MaINSettings _settings;

    internal ModelContext(MaINSettings settings)
    {
        _settings = settings;
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
    
    public Task<ModelContext> DownloadAsync(string model) => Task.FromResult(this); //TODO
    
    public ModelContext Download(string model) => this;
    
    public Task<ModelContext> DownloadAsync(string model, string url) => Task.FromResult(this);
    
    public ModelContext Download(string model, string url) => this;

    public ModelContext LoadToCache(Model model) => this;

    private string ResolvePath(string? settingsModelsPath) =>
        settingsModelsPath 
        ?? Environment.GetEnvironmentVariable("MaIN_ModelsPath") 
        ?? throw new Exception("Models path not found");
}