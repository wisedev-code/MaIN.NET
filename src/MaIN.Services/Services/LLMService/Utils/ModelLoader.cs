using System.Collections.Concurrent;
using LLama;
using LLama.Common;

namespace MaIN.Services.Services.LLMService.Utils;

public static class ModelLoader
{
    private static readonly ConcurrentDictionary<string, LLamaWeights> ModelCache = new();

    public static async Task<LLamaWeights> GetOrLoadModelAsync(string path, string modelKey)
    {
        if (ModelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = await LLamaWeights.LoadFromFileAsync(parameters);
        return ModelCache.GetOrAdd(modelKey, loadedModel);
    }
    
    public static LLamaWeights GetOrLoadModel(string path, string modelKey)
    {
        if (ModelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = LLamaWeights.LoadFromFile(parameters);
        return ModelCache.GetOrAdd(modelKey, loadedModel);
    }
}