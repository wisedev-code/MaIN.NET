using MaIN.Domain.Exceptions;
using System.Collections.Concurrent;
using System.Reflection;

namespace MaIN.Domain.Models.Abstract;

public static class ModelRegistry
{
    private static readonly ConcurrentDictionary<string, AIModel> _models = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized = false;
    private static readonly object _lock = new();

    static ModelRegistry()
    {
        Initialize();
    }

    private static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            // Reflection but only at startup to register all available models
            // Skip abstract, generic types, and Generic* classes (they're for runtime registration)
            var modelTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(AIModel)) 
                            && !t.IsAbstract 
                            && !t.IsGenericType
                            && !t.Name.StartsWith("Generic"));

            foreach (var type in modelTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is AIModel instance)
                    {
                        Register(instance);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not register model type {type.Name}: {ex.Message}");
                }
            }
            
            _initialized = true;
        }
    }

    /// <summary>
    /// Registers a custom model at runtime (e.g., dynamically loaded GGUF file).
    /// </summary>
    public static void Register(AIModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(model));
        }

        var normalizedId = NormalizeId(model.Id);

        if (!_models.TryAdd(normalizedId, model))
        {
            throw new InvalidOperationException($"Model with ID '{model.Id}' is already registered.");
        }
    }

    /// <summary>
    /// Registers a custom model at runtime, replacing existing if present.
    /// </summary>
    public static void RegisterOrReplace(AIModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(model));
        }

        var normalizedId = NormalizeId(model.Id);
        _models[normalizedId] = model;
    }

    /// <summary>
    /// Gets a model by its ID.
    /// </summary>
    public static AIModel GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(id));
        }

        var normalizedId = NormalizeId(id);

        if (_models.TryGetValue(normalizedId, out var model))
        {
            return model;
        }

        var availableIds = string.Join(", ", _models.Keys.Take(10));
        throw new KeyNotFoundException($"Model with ID '{id}' not found. Available models (first 10): {availableIds}");
    }

    /// <summary>
    /// Tries to get a model by its ID.
    /// </summary>
    public static bool TryGetById(string id, out AIModel? model)
    {
        model = null;
        
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var normalizedId = NormalizeId(id);
        return _models.TryGetValue(normalizedId, out model);
    }

    /// <summary>
    /// Gets a local model by its filename.
    /// </summary>
    public static LocalModel? GetByFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return _models.Values
            .OfType<LocalModel>()
            .FirstOrDefault(m => m.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all registered models.
    /// </summary>
    public static IEnumerable<AIModel> GetAll() => _models.Values;

    /// <summary>
    /// Gets all local models.
    /// </summary>
    public static IEnumerable<LocalModel> GetAllLocal() => _models.Values.OfType<LocalModel>();

    /// <summary>
    /// Gets all cloud models.
    /// </summary>
    public static IEnumerable<CloudModel> GetAllCloud() => _models.Values.OfType<CloudModel>();

    /// <summary>
    /// Checks if a model with the given ID exists.
    /// </summary>
    public static bool Exists(string id) =>
        !string.IsNullOrWhiteSpace(id) && _models.ContainsKey(NormalizeId(id));

    /// <summary>
    /// Removes a model from the registry (useful for dynamically loaded models).
    /// </summary>
    public static bool Unregister(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _models.TryRemove(NormalizeId(id), out _);
    }

    private static string NormalizeId(string id) => id.Trim();
}
