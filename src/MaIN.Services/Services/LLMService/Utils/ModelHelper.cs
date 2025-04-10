using System.Text.RegularExpressions;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;

namespace MaIN.Services.Utils;

/// <summary>
/// Helper for model-related operations
/// </summary>
public static class ModelHelper
{
    private static readonly Regex ModelRegex = new(@"model-([a-zA-Z0-9_-]+)", RegexOptions.Compiled);
    
    /// <summary>
    /// Gets model by name
    /// </summary>
    public static Model GetModel(string modelName)
    {
        return KnownModels.GetModel(modelName);
    }
    
    /// <summary>
    /// Gets model by file name
    /// </summary>
    public static Model GetModelByFileName(string fileName)
    {
        var match = ModelRegex.Match(fileName);
        if (!match.Success || match.Groups.Count < 2)
        {
            return null;
        }
        
        var modelName = match.Groups[1].Value;
        return new Model
        {
            Name = modelName,
            FileName = fileName
        };
    }
    
    /// <summary>
    /// Gets embedding model
    /// </summary>
    public static Model GetEmbeddingModel()
    {
        return KnownModels.GetEmbeddingModel();
    }
}