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
    
    public static void ClearCache()
    {
        foreach (var model in ModelCache.Values)
        {
            try
            {
                model.Dispose();
            }
            catch
            {
                // Continue if a model cannot be disposed
            }
        }
        ModelCache.Clear();
    }
}