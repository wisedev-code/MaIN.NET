using LLama;
using LLama.Common;
using LLama.Native;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using System.Text;
using LLama.Abstractions;
using MaIN.Services.Services.LLMService.Memory.Embeddings;

namespace LLamaSharp.KernelMemory
{
    /// <summary>
    /// Provides text embedding generation for LLamaSharp. - Clone in MaIN.Package to support embeddings in custom manner
    /// </summary>
    public sealed class LLamaSharpTextEmbeddingMaINClone
        : ITextEmbeddingGenerator, IDisposable
    {
        private LLamaWeights? _weights;
        private readonly bool _ownsWeights;

        public readonly LLamaEmbedderMaINClone _embedder;
        private readonly bool _ownsEmbedder;
        public ModelParams? @params;

        /// <inheritdoc/>
        public int MaxTokens { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LLamaSharpTextEmbeddingMaINClone"/> class.
        /// </summary>
        /// <param name="config">The configuration for LLamaSharp.</param>
        public LLamaSharpTextEmbeddingMaINClone(LLamaSharpConfig config)
        {
            MaxTokens = (int?)config.ContextSize ?? 2048;

            @params = new ModelParams(config.ModelPath)
            {
                ContextSize = config?.ContextSize ?? 2048,
                GpuLayerCount = config?.GpuLayerCount ?? 20,
                MainGpu = config?.MainGpu ?? 0,
                SplitMode = config?.SplitMode ?? LLama.Native.GPUSplitMode.Layer,
                BatchSize = 1024,
                UBatchSize = 1024,
                FlashAttention = true,
                UseMemorymap = true,
                PoolingType = LLamaPoolingType.Mean,
            };

            _weights = LLamaWeights.LoadFromFile(@params);
            _embedder = new LLamaEmbedderMaINClone(_weights, @params);
            _ownsWeights = true;
            _ownsEmbedder = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LLamaSharpTextEmbeddingMaINClone"/> class from reused weights.
        /// </summary>
        /// <param name="config">The configuration for LLamaSharp.</param>
        /// <param name="weights">A LLamaWeights object.</param>
        public LLamaSharpTextEmbeddingMaINClone(LLamaSharpConfig config, LLamaWeights weights)
        {
            MaxTokens = (int?)config.ContextSize ?? 2048;

            @params = new ModelParams(config.ModelPath)
            {
                ContextSize = config?.ContextSize ?? 2048,
                GpuLayerCount = config?.GpuLayerCount ?? 20,
                MainGpu = config?.MainGpu ?? 0,
                SplitMode = config?.SplitMode ?? LLama.Native.GPUSplitMode.Layer,
                BatchSize = 1024,
                UBatchSize = 1024,
                FlashAttention = true,
                UseMemorymap = true,
                PoolingType = LLamaPoolingType.Mean,
            };
            _weights = weights;
            _embedder = new LLamaEmbedderMaINClone(_weights, @params);
            _ownsWeights = true;
            _ownsEmbedder = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LLamaSharpTextEmbeddingMaINClone"/> class from reused embedder.
        /// </summary>
        /// <param name="embedder">A LLamaEmbedder object.</param>
        public LLamaSharpTextEmbeddingMaINClone(LLamaEmbedderMaINClone embedder)
        {
            _embedder = embedder;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_ownsWeights)
            {
                _weights?.Dispose();
            }
            if (_ownsEmbedder)
            {
                _embedder.Dispose();
            }
        }

        /// <inheritdoc/>
        public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            var embeddings = await _embedder.GetEmbeddings(text, cancellationToken);
            return new Embedding(embeddings.First());
        }

        /// <summary>
        /// Count the tokens in the input text
        /// </summary>
        /// <param name="text">input text</param>
        /// <param name="parameters">context parameters</param>
        /// <returns></returns>
        public int CountTokens(string text)
        {
            return _weights!.Tokenize(text, true, special: true, Encoding.UTF8).Length;
        }

        /// <summary>
        /// Get the list of tokens for the input text
        /// </summary>
        /// <param name="text">Input string to be tokenized</param>
        /// <param name="parameters">Context parameters</param>
        /// <returns>Read-only list of tokens for the input test</returns>
        /// <remarks>
        /// It throws if text is null and Includes empty stop token because addBos is left true to be consistent with the CountTokens implementation.</remarks>
        /// <see cref="CountTokens(string, IContextParams)"/>
        public IReadOnlyList<string> GetTokens(string text)
        {
            var numericTokens = _weights!.Tokenize(text, true, special: true, Encoding.UTF8);
            var decoder = new StreamingTokenDecoder(Encoding.UTF8, _weights);
            return numericTokens.Select(x => { decoder.Add(x); return decoder.Read(); }).ToList();
        }
    }
}
