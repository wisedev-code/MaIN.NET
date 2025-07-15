// Decompiled with JetBrains decompiler
// Type: LLamaSharp.KernelMemory.LlamaSharpTextGenerator
// Assembly: LLamaSharp.KernelMemory, Version=0.24.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F3EEF66F-D255-4CA9-972B-2116B924F65B
// Assembly location: /Users/pstach/.nuget/packages/llamasharp.kernel-memory/0.24.0/lib/net8.0/LLamaSharp.KernelMemory.dll

using System.Diagnostics.CodeAnalysis;
using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

[Experimental("KMEXP00")]
public sealed class LlamaSharpTextGen : ITextGenerator, ITextTokenizer, IDisposable
{
    private readonly BatchedExecutor _executor;
    private readonly LLamaWeights _weights;
    private readonly bool _ownsWeights;
    private readonly LLamaContext _context;
    private readonly bool _ownsContext;
    private readonly InferenceParams? _defaultInferenceParams;

    public int MaxTokenTotal { get; }

    public LlamaSharpTextGen(LLamaSharpConfig config)
    {
        ModelParams modelParams = new ModelParams(config.ModelPath);
        uint? contextSize = config?.ContextSize;
        modelParams.ContextSize = contextSize.GetValueOrDefault(2048U);
        modelParams.GpuLayerCount = (config?.GpuLayerCount).GetValueOrDefault(20);
        modelParams.MainGpu = config != null ? config.MainGpu : 0;
        modelParams.SplitMode = config != null ? config.SplitMode : GPUSplitMode.None;
        ModelParams @params = modelParams;
        _weights = LLamaWeights.LoadFromFile(@params);
        _context = _weights.CreateContext(@params);
        _executor = new BatchedExecutor(_weights, @params);
        _defaultInferenceParams = config.DefaultInferenceParams;
        _ownsWeights = _ownsContext = true;
        contextSize = @params.ContextSize;
        MaxTokenTotal = (int)contextSize.Value;
    }

    public LlamaSharpTextGen(
        LLamaWeights weights,
        LLamaContext context,
        BatchedExecutor? executor = null,
        InferenceParams? inferenceParams = null)
    {
        _weights = weights;
        _context = context;
        _executor = executor ?? new BatchedExecutor(_weights, _context.Params);
        _defaultInferenceParams = inferenceParams;
        MaxTokenTotal = (int)_context.ContextSize;
    }

    public void Dispose()
    {
        if (_ownsWeights)
            _weights.Dispose();
        if (!_ownsContext)
            return;
        _context.Dispose();
    }

    public IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        return _executor
            .InferAsync(prompt,
                OptionsToParams(options, _defaultInferenceParams),
                cancellationToken)
            .Select(
                (Func<string, GeneratedTextContent>)(a => new GeneratedTextContent(a)));
    }

    private static InferenceParams OptionsToParams(
        TextGenerationOptions options,
        InferenceParams? defaultParams)
    {
        if (defaultParams != null)
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
        return new InferenceParams
        {
            AntiPrompts = options.StopSequences.ToList().AsReadOnly(),
            MaxTokens = options.MaxTokens.GetValueOrDefault(1024),
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = (float)options.Temperature,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                TopP = (float)options.NucleusSampling
            }
        };
    }

    public int CountTokens(string text) => _context.Tokenize(text, special: true).Length;

    public IReadOnlyList<string> GetTokens(string text)
    {
        LLamaToken[] source = _context.Tokenize(text, special: true);
        StreamingTokenDecoder decoder = new StreamingTokenDecoder(_context);
        Func<LLamaToken, string> selector = (Func<LLamaToken, string>)(x =>
        {
            decoder.Add(x);
            return decoder.Read();
        });
        return source.Select<LLamaToken, string>(selector)
            .ToList();
    }
}