using LLama;
using LLama.Extensions;

namespace MaIN.Services.Services.LLMService.Memory.Embeddings;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LLama.Abstractions;
using LLama.Exceptions;
using LLama.Native;
using Microsoft.Extensions.Logging;

/// <summary>
/// Generate high dimensional embedding vectors from text, Clone in MaIN.NET
/// </summary>
public sealed class LLamaEmbedderMaINClone
    : IDisposable
{
    public LLamaWeights _weights;
    private readonly IContextParams _params;
    private readonly ILogger? _logger;

    /// <summary>
    /// Dimension of embedding vectors
    /// </summary>
    public int EmbeddingSize { get; }

    /// <summary>
    /// Create a new embedder, using the given LLamaWeights
    /// </summary>
    /// <param name="weights"></param>
    /// <param name="params"></param>
    /// <param name="logger"></param>
    public LLamaEmbedderMaINClone(LLamaWeights weights, IContextParams @params, ILogger? logger = null)
    {
        if (@params.UBatchSize != @params.BatchSize)
            throw new ArgumentException("For non-causal models, batch size must be equal to ubatch size", nameof(@params));
        if (weights.NativeHandle is { HasEncoder: true, HasDecoder: true })
            throw new NotSupportedException("Computing embeddings in encoder-decoder models is not supported");

        // Create context only to read EmbeddingSize, then dispose immediately
        // (matches LLamaSharp 0.26.0 LLamaEmbedder pattern)
        using (var tempContext = weights.CreateContext(@params, logger))
        {
            EmbeddingSize = tempContext.EmbeddingSize;
        }

        _weights = weights;
        _params = @params;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// Get high dimensional embedding vectors for the given text. Depending on the pooling type used when constructing
    /// this <see cref="LLamaEmbedderMaINClone"/> this may return an embedding vector per token, or one single embedding vector for the entire string.
    /// </summary>
    /// <remarks>Embedding vectors are not normalized, consider using one of the extensions in <see cref="SpanNormalizationExtensions"/>.</remarks>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="RuntimeError"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<IReadOnlyList<float[]>> GetEmbeddings(string input, CancellationToken cancellationToken = default) =>
        (await GetEmbeddingsWithTokenCount(input, cancellationToken).ConfigureAwait(false)).Embeddings;

    private async Task<(IReadOnlyList<float[]> Embeddings, int Tokens)> GetEmbeddingsWithTokenCount(string input, CancellationToken cancellationToken = default)
    {
        // Create a fresh context for each embedding call (0.26.0 pattern)
        using var context = _weights.CreateContext(_params, _logger);
        NativeApi.llama_set_embeddings(context.NativeHandle, true);

        var tokens = context.Tokenize(input, special: true);
        if (tokens.Length > context.ContextSize)
            throw new ArgumentException($"Embedding prompt is longer than the context window ({tokens.Length} > {context.ContextSize})", nameof(input));

        cancellationToken.ThrowIfCancellationRequested();

        // Evaluate prompt in batch-size chunks
        var n_past = 0;
        var batch = new LLamaBatch();
        var batchSize = (int)context.Params.BatchSize;
        for (var i = 0; i < tokens.Length; i += batchSize)
        {
            var n_eval = tokens.Length - i;
            if (n_eval > batchSize)
                n_eval = batchSize;

            batch.Clear();
            batch.AddRange(tokens.AsSpan(i, n_eval), n_past, LLamaSeqId.Zero, true);
            n_past += n_eval;

            // Run model
            switch (context.NativeHandle.ModelHandle.HasEncoder, context.NativeHandle.ModelHandle.HasDecoder)
            {
                case (true, false):
                    {
                        var result = await context.EncodeAsync(batch, cancellationToken);
                        if (result != EncodeResult.Ok)
                            throw new RuntimeError($"Failed to encode: {result}");
                        break;
                    }

                case (false, true):
                    {
                        var result = await context.DecodeAsync(batch, cancellationToken);
                        if (result != DecodeResult.Ok)
                            throw new RuntimeError($"Failed to decode: {result}");
                        break;
                    }

                default:
                    throw new NotSupportedException("Unsupported model type");
            }
        }

        // Extract results
        var poolingType = context.NativeHandle.PoolingType;
        var resultsCount = poolingType == LLamaPoolingType.None ? tokens.Length : 1;
        var results = new List<float[]>(resultsCount);
        results.Add(context.NativeHandle.GetEmbeddingsSeq(LLamaSeqId.Zero).ToArray());

        // Normalize the embeddings vector
        foreach (var embedding in results)
        {
            embedding.EuclideanNormalization();
        }

        return (results, tokens.Length);
    }
}
