using MaIN.Domain.Models;

namespace MaIN.Core.Hub.Contexts.Interfaces.ModelContext;

public interface IModelContext
{
    /// <summary>
    /// Retrieves a complete list of all available models in the system. This method returns all known models that
    /// can be used within the MaIN framework.
    /// </summary>
    /// <returns>A list of <see cref="Model"/> containing all available models in the system</returns>
    List<Model> GetAll();

    /// <summary>
    /// Retrieves information about a specific model by its name. This method allows you to get detailed information about a particular model,
    /// including its configuration and metadata.
    /// </summary>
    /// <param name="model">The name of the model to retrieve.</param>
    /// <returns>A <see cref="Model"/> object containing the model's information and configuration.</returns>
    Model GetModel(string model);

    /// <summary>
    /// Retrieves the designated embedding model used for generating vector representations of text.
    /// </summary>
    /// <returns>A Model object representing the embedding model.</returns>
    Model GetEmbeddingModel();

    /// <summary>
    /// Checks whether a specific model exists locally on the filesystem.
    /// </summary>
    /// <param name="modelName">The name of the model to check for existence.</param>
    /// <returns>True if the model file exists locally; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown if the model name is null or empty.</exception>
    bool Exists(string modelName);

    /// <summary>
    /// Asynchronously downloads a known model from its configured download URL.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the download operation.</param>
    /// <returns>A task that represents the asynchronous download operation, returning the IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if the model name is null or empty.</exception>
    Task<IModelContext> DownloadAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads a custom model from a specified URL.
    /// </summary>
    /// <param name="model">The name to assign to the downloaded model.</param>
    /// <param name="url">The URL from which to download the model.</param>
    /// <returns>A task that represents the asynchronous download operation, returning the IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if the model name or URL is null or empty.</exception>
    Task<IModelContext> DownloadAsync(string model, string url);

    /// <summary>
    /// Synchronously downloads a known model from its configured download URL.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <returns>The IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if the model name is null or empty.</exception>
    [Obsolete("Use DownloadAsync instead")]
    IModelContext Download(string modelName);

    /// <summary>
    /// Synchronously downloads a custom model from a specified URL.
    /// </summary>
    /// <param name="model">The name to assign to the downloaded model.</param>
    /// <param name="url">The URL from which to download the model.</param>
    /// <returns>The IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if the model name or URL is null or empty.</exception>
    [Obsolete("Use DownloadAsync instead")]
    IModelContext Download(string model, string url);

    /// <summary>
    /// Loads a model into the memory cache for faster access during inference operations.
    /// </summary>
    /// <param name="model">The Model object to load into cache.</param>
    /// <returns>The IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the model parameter is null.</exception>
    IModelContext LoadToCache(Model model);

    /// <summary>
    /// Asynchronously loads a model into the memory cache for faster access during inference operations.
    /// </summary>
    /// <param name="model">The Model object to load into a cache.</param>
    /// <returns>A task that represents the asynchronous load operation, returning the IModelContext instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the model parameter is null.</exception>
    Task<IModelContext> LoadToCacheAsync(Model model);
}