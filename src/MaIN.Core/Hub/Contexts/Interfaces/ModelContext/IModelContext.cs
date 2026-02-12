using MaIN.Domain.Models.Abstract;

namespace MaIN.Core.Hub.Contexts.Interfaces.ModelContext;

public interface IModelContext
{
    /// <summary>
    /// Retrieves an enumerable collection of all available models in the system. This method returns all known models that
    /// can be used within the MaIN framework.
    /// </summary>
    /// <returns>A list of <see cref="AIModel"/> containing all available models in the system</returns>
    IEnumerable<AIModel> GetAll();

    /// <summary>
    /// Retrieves all local models available in the data store.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="LocalModel"/> instances representing all local models. The collection is
    /// empty if no local models are found.</returns>
    IEnumerable<LocalModel> GetAllLocal();

    /// <summary>
    /// Retrieves information about a specific model by its id. This method allows you to get detailed information about a particular model,
    /// including its configuration and metadata.
    /// </summary>
    /// <param name="modelId">The id of the model to retrieve.</param>
    /// <returns>A <see cref="AIModel"/> object containing the model's information and configuration.</returns>
    AIModel GetModel(string modelId);

    /// <summary>
    /// Retrieves the designated embedding model used for generating vector representations of text. This is typically used
    /// for semantic search, similarity calculations, and other NLP tasks that require text embeddings.
    /// </summary>
    /// <returns>A <see cref="AIModel"/> object representing the embedding model.</returns>
    AIModel GetEmbeddingModel();

    /// <summary>
    /// Checks whether a specific model exists locally on the filesystem. This method verifies if the model file is present
    /// and accessible before attempting to use it.
    /// </summary>
    /// <param name="modelId">The id of the model to check for existence.</param>
    /// <returns>A boolean value indicating whether the model file exists locally.</returns>
    bool IsDownloaded(string modelId);

    /// <summary>
    /// Asynchronously downloads a known model from its configured download URL. This method handles the complete download process
    /// with progress tracking and error handling.
    /// </summary>
    /// <param name="modelId">The id of the model to download.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the download operation.</param>
    /// <returns>A task that represents the asynchronous download operation that completes when the download finishes,
    /// returning the context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> DownloadAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a known local model is downloaded before use. If the model is already present on disk the call
    /// returns immediately; if not, the model is downloaded. Cloud models are silently skipped.
    /// Thread-safe: concurrent calls for the same model will not trigger duplicate downloads.
    /// </summary>
    /// <param name="modelId">The id of the model to ensure is downloaded.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the download operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the context instance implementing
    /// <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> EnsureDownloadedAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a known local model is downloaded before use using a strongly-typed model reference.
    /// If the model is already present on disk the call returns immediately; if not, the model is downloaded.
    /// Cloud models are silently skipped.
    /// Thread-safe: concurrent calls for the same model will not trigger duplicate downloads.
    /// </summary>
    /// <typeparam name="TModel">A <see cref="LocalModel"/> type with a parameterless constructor.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token to abort the download operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the context instance implementing
    /// <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> EnsureDownloadedAsync<TModel>(CancellationToken cancellationToken = default) where TModel : LocalModel, new();

    /// <summary>
    /// Loads a model into the memory cache for faster access during inference operations. This method preloads the model to avoid loading
    /// delays when the model is first used in chat sessions.
    /// </summary>
    /// <param name="model">The Model object to load into a cache.</param>
    /// <returns>The context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    IModelContext LoadToCache(LocalModel model);

    /// <summary>
    /// Asynchronously loads a model into the memory cache for faster access during inference operations. This is the non-blocking
    /// version of cache loading that allows other operations to continue while the model loads.
    /// </summary>
    /// <param name="model">The <see cref="AIModel"/> object to load into a cache.</param>
    /// <returns>A task that represents the asynchronous operation that completes when the model is loaded into cache,
    /// returning the context instance implementing <see cref="IModelContext"/> for method chaining.</returns>
    Task<IModelContext> LoadToCacheAsync(LocalModel model);
}
