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
    /// Retrieves the designated embedding model used for generating vector representations of text. This is typically used
    /// for semantic search, similarity calculations, and other NLP tasks that require text embeddings.
    /// </summary>
    /// <returns>A <see cref="Model"/> object representing the embedding model.</returns>
    Model GetEmbeddingModel();

    /// <summary>
    /// Checks whether a specific model exists locally on the filesystem. This method verifies if the model file is present
    /// and accessible before attempting to use it.
    /// </summary>
    /// <param name="modelName">The name of the model to check for existence.</param>
    /// <returns>A boolean value indicating whether the model file exists locally.</returns>
    bool Exists(string modelName);

    /// <summary>
    /// Asynchronously downloads a known model from its configured download URL. This method handles the complete download process
    /// with progress tracking and error handling.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the download operation.</param>
    /// <returns>A task that represents the asynchronous download operation that completes when the download finishes,
    /// returning the context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> DownloadAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads a custom model from a specified URL. This method allows downloading models that are not part
    /// of the known models collection, adding them to the system after download.
    /// </summary>
    /// <param name="model">The name to assign to the downloaded model.</param>
    /// <param name="url">The URL from which to download the model.</param>
    /// <returns>A task that represents the asynchronous download operation that completes when the download finishes,
    /// returning the context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> DownloadAsync(string model, string url);

    /// <summary>
    /// Synchronously downloads a known model from its configured download URL. This is the blocking version of the download operation
    /// with progress tracking.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <returns>The context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    [Obsolete("Use DownloadAsync instead")]
    IModelContext Download(string modelName);

    /// <summary>
    /// Synchronously downloads a custom model from a specified URL. This method provides blocking download functionality
    /// for custom models not in the known models collection.
    /// </summary>
    /// <param name="model">The name to assign to the downloaded model.</param>
    /// <param name="url">The URL from which to download the model.</param>
    /// <returns>The context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    [Obsolete("Use DownloadAsync instead")]
    IModelContext Download(string model, string url);

    /// <summary>
    /// Loads a model into the memory cache for faster access during inference operations. This method preloads the model to avoid loading
    /// delays when the model is first used in chat sessions.
    /// </summary>
    /// <param name="model">The Model object to load into a cache.</param>
    /// <returns>The context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    IModelContext LoadToCache(Model model);

    /// <summary>
    /// Asynchronously loads a model into the memory cache for faster access during inference operations. This is the non-blocking
    /// version of cache loading that allows other operations to continue while the model loads.
    /// </summary>
    /// <param name="model">The <see cref="Model"/> object to load into a cache.</param>
    /// <returns>A task that represents the asynchronous operation that completes when the model is loaded into cache,
    /// returning the context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> LoadToCacheAsync(Model model);
}