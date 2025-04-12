using LLama;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public static class KernelMemoryLlamaExtensions 
{
    private static LLamaWeights? _sharedWeights;
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        string path, 
        string modelName, 
        out LlamaSharpTextGenerator generator)
    {
        var model = ModelLoader.GetOrLoadModelAsync(path, modelName).Result;

        var parameters = new ModelParams(Path.Combine(path, modelName))
        {
            ContextSize = 2048,
            GpuLayerCount = 20
        };

        _sharedWeights ??= model;

        var context = model.CreateContext(parameters);
        var executor = new StatelessExecutor(model, parameters);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"] 
        };

        generator = new LlamaSharpTextGenerator(model, context, executor, inferenceParams);

        builder.WithLLamaSharpTextGeneration(
            new LLamaSharp.KernelMemory.LlamaSharpTextGenerator(
                _sharedWeights,
                context,
                executor,
                inferenceParams
            )
        );
        
        builder.AddSingleton<ITextGenerator>(generator);
        
        return builder;
    }
    
    public sealed class LlamaSharpTextGenerator : ITextGenerator, IDisposable
    {
        private readonly StatelessExecutor _executor;
        private readonly LLamaWeights _weights;
        private readonly LLamaContext _context;
        private readonly InferenceParams? _defaultInferenceParams;

        public int MaxTokenTotal { get; }

        public LlamaSharpTextGenerator(
            LLamaWeights weights,
            LLamaContext context,
            StatelessExecutor? executor = null,
            InferenceParams? inferenceParams = null)
        {
            _weights = weights;
            _context = context;
            _executor = executor ?? new StatelessExecutor(_weights, _context.Params);
            _defaultInferenceParams = inferenceParams;
            MaxTokenTotal = (int)_context.ContextSize;
        }

        public void Dispose()
        {
            _weights.Dispose();
            _context.Dispose();
        }

        public IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
            string prompt, 
            TextGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return _executor
                .InferAsync(prompt, OptionsToParams(options, _defaultInferenceParams), cancellationToken: cancellationToken)
                .Select(a => new GeneratedTextContent(a));
        }

        public int CountTokens(string text) => _context.Tokenize(text, special: true).Length;

        public IReadOnlyList<string> GetTokens(string text)
        {
            var tokens = _context.Tokenize(text, special: true);
            var decoder = new StreamingTokenDecoder(_context);
            
            return tokens
                .Select(token => {
                    decoder.Add(token);
                    return decoder.Read();
                })
                .ToList();
        }

        private static InferenceParams OptionsToParams(
            TextGenerationOptions options,
            InferenceParams? defaultParams)
        {
            if (defaultParams != null)
            {
                return defaultParams with
                {
                    AntiPrompts = defaultParams.AntiPrompts
                        .Concat(options.StopSequences).ToList().AsReadOnly(),
                    MaxTokens = options.MaxTokens ?? defaultParams.MaxTokens,
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = (float)options.Temperature,
                        FrequencyPenalty = (float)options.FrequencyPenalty,
                        PresencePenalty = (float)options.PresencePenalty,
                        TopP = (float)options.NucleusSampling
                    }
                };
            }
            
            return new InferenceParams
            {
                AntiPrompts = options.StopSequences.ToList().AsReadOnly(),
                MaxTokens = options.MaxTokens ?? 1024,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = (float)options.Temperature,
                    FrequencyPenalty = (float)options.FrequencyPenalty,
                    PresencePenalty = (float)options.PresencePenalty,
                    TopP = (float)options.NucleusSampling
                }
            };
        }
    }
}