// Decompiled with JetBrains decompiler
// Type: LLamaSharp.KernelMemory.LlamaSharpTextGenerator
// Assembly: LLamaSharp.KernelMemory, Version=0.24.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F3EEF66F-D255-4CA9-972B-2116B924F65B
// Assembly location: /Users/pstach/.nuget/packages/llamasharp.kernel-memory/0.24.0/lib/net8.0/LLamaSharp.KernelMemory.dll

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
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
    private ISamplingPipeline CreateSampler(InferenceParams interferenceParams)
    {
        return interferenceParams.SamplingPipeline;
    }
public async IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
    string prompt,
    TextGenerationOptions options,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var parameters = OptionsToParams(options, _defaultInferenceParams);
    
    // Create conversation from the executor (assuming you have a way to create this)
    var conversation = _executor.Create();
    conversation.Prompt(_weights.Tokenize(prompt, true, false, Encoding.UTF8));

    using var sampler = CreateSampler(_defaultInferenceParams!);
    var decoder = new StreamingTokenDecoder(_executor.Context);
    
    var maxTokens = GetMaxTokensFromOptions(options);
    var isComplete = false;
    
    for (var i = 0; i < maxTokens && !isComplete; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var decodeResult = await _executor.Infer(cancellationToken);
        
        if (decodeResult == DecodeResult.NoKvSlot || decodeResult == DecodeResult.Error)
        {
            yield break;
        }
        
        if (!conversation.RequiresSampling)
            continue;
            
        var token = conversation.Sample(sampler);
        var vocab = _executor.Context.NativeHandle.ModelHandle.Vocab;
        
        if (token.IsEndOfGeneration(vocab))
        {
            isComplete = true;
        }
        else
        {
            decoder.Add(token);
            var tokenText = decoder.Read();
            
            conversation.Prompt(token);
            
            if (!string.IsNullOrEmpty(tokenText))
            {
                await Task.Yield();
                yield return new GeneratedTextContent(tokenText);
            }
        }
    }
}

private int GetMaxTokensFromOptions(TextGenerationOptions options)
{
    // Extract max tokens from your options, defaulting to int.MaxValue if not specified
    // You'll need to implement this based on your TextGenerationOptions structure
    return options?.MaxTokens == -1 ? int.MaxValue : (options?.MaxTokens ?? int.MaxValue);
}

private object CreateSampler(object parameters)
{
    // You'll need to implement this based on how you create samplers in your system
    // This should match the sampler creation logic from your existing code
    throw new NotImplementedException("Implement based on your sampler creation logic");
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