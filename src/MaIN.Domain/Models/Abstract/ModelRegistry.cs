using MaIN.Domain.Exceptions;
using System.Reflection;

namespace MaIN.Domain.Models.Abstract;

public static class ModelRegistry
{
    private static readonly Dictionary<string, AIModel> _models = new(StringComparer.OrdinalIgnoreCase);

    static ModelRegistry()
    {
        // Reflection but only at startup to register all available models
        var modelTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AIModel)) && !t.IsAbstract);

        foreach (var type in modelTypes)
        {
            if (Activator.CreateInstance(type) is AIModel instance)
            {
                if (string.IsNullOrWhiteSpace(instance.Id))
                {
                    throw new ModelException($"Model type {type.Name} has an empty or null Id.");
                }

                var normalizedId = instance.Id.Trim().Replace(':', '-');

                if (!_models.TryAdd(normalizedId, instance))
                {
                    throw new InvalidOperationException($"Duplicate Model ID detected: '{normalizedId}'. Classes: {_models[normalizedId].GetType().Name} and {type.Name}");
                }
            }
        }
    }

    public static AIModel GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(id));
        }

        if (_models.TryGetValue(id.Trim(), out var model))
        {
            return model;
        }

        var availableIds = string.Join(", ", _models.Keys);
        throw new KeyNotFoundException($"Model with ID '{id}' not found. Available models: {availableIds}");
    }

    public static IEnumerable<AIModel> GetAll() => _models.Values;

    public static bool Exists(string id) =>
        !string.IsNullOrWhiteSpace(id) && _models.ContainsKey(id.Trim());
}
