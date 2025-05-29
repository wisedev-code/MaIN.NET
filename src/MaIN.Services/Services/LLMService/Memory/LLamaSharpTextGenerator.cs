// Decompiled with JetBrains decompiler
// Type: LLamaSharp.KernelMemory.LlamaSharpTextGenerator
// Assembly: LLamaSharp.KernelMemory, Version=0.24.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F3EEF66F-D255-4CA9-972B-2116B924F65B
// Assembly location: C:\Users\stach\.nuget\packages\llamasharp.kernel-memory\0.24.0\lib\net8.0\LLamaSharp.KernelMemory.dll

using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using LLama.Batched;
using LLamaSharp.KernelMemory;

#nullable enable

[Experimental("KMEXP00")]
public sealed class LLamaSharpTextGenerator : ITextGenerator, ITextTokenizer, IDisposable
{
  private readonly BatchedExecutor _executor;
  private readonly LLamaWeights _weights;
  private readonly bool _ownsWeights;
  private readonly LLamaContext _context;
  private readonly bool _ownsContext;
  private readonly InferenceParams? _defaultInferenceParams;

  public int MaxTokenTotal { get; }

  public LLamaSharpTextGenerator(LLamaSharpConfig config)
  {
    ModelParams modelParams = new ModelParams(config.ModelPath);
    uint? contextSize = (uint?) config?.ContextSize;
    modelParams.ContextSize = new uint?(contextSize ?? 2048U /*0x0800*/);
    modelParams.GpuLayerCount = (int?) config?.GpuLayerCount ?? 20;
    modelParams.MainGpu = config != null ? config.MainGpu : 0;
    modelParams.SplitMode = new GPUSplitMode?(config != null ? config.SplitMode : GPUSplitMode.None);
    ModelParams @params = modelParams;
    this._weights = LLamaWeights.LoadFromFile((IModelParams) @params);
    this._context = this._weights.CreateContext((IContextParams) @params);
    this._executor = new BatchedExecutor(this._weights, (IContextParams) @params);
    this._defaultInferenceParams = config.DefaultInferenceParams;
    this._ownsWeights = this._ownsContext = true;
    contextSize = @params.ContextSize;
    this.MaxTokenTotal = (int) contextSize.Value;
  }

  public LLamaSharpTextGenerator(
    LLamaWeights weights,
    LLamaContext context,
    BatchedExecutor? executor = null,
    InferenceParams? inferenceParams = null)
  {
    this._weights = weights;
    this._context = context;
    this._executor = executor ?? new BatchedExecutor(this._weights, this._context.Params);
    this._defaultInferenceParams = inferenceParams;
    this.MaxTokenTotal = (int) this._context.ContextSize;
  }

  public void Dispose()
  {
    if (this._ownsWeights)
      this._weights.Dispose();
    if (!this._ownsContext)
      return;
    this._context.Dispose();
  }

  public IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(
    string prompt,
    TextGenerationOptions options,
    CancellationToken cancellationToken = default (CancellationToken))
  {
    //batchedExecutor to generate text TODO
    return this._executor.InferAsync(prompt, (IInferenceParams) LLamaSharpTextGenerator.OptionsToParams(options, this._defaultInferenceParams), cancellationToken).Select<string, GeneratedTextContent>((Func<string, GeneratedTextContent>) (a => new GeneratedTextContent(a)));
  }

  private static InferenceParams OptionsToParams(
    TextGenerationOptions options,
    InferenceParams? defaultParams)
  {
    if (defaultParams != (InferenceParams) null)
      return defaultParams with
      {
        AntiPrompts = (IReadOnlyList<string>) defaultParams.AntiPrompts.Concat<string>((IEnumerable<string>) options.StopSequences).ToList<string>().AsReadOnly(),
        MaxTokens = options.MaxTokens ?? defaultParams.MaxTokens,
        SamplingPipeline = (ISamplingPipeline) new DefaultSamplingPipeline()
        {
          Temperature = (float) options.Temperature,
          FrequencyPenalty = (float) options.FrequencyPenalty,
          PresencePenalty = (float) options.PresencePenalty,
          TopP = (float) options.NucleusSampling
        }
      };
    return new InferenceParams()
    {
      AntiPrompts = (IReadOnlyList<string>) options.StopSequences.ToList<string>().AsReadOnly(),
      MaxTokens = options.MaxTokens ?? 1024 /*0x0400*/,
      SamplingPipeline = (ISamplingPipeline) new DefaultSamplingPipeline()
      {
        Temperature = (float) options.Temperature,
        FrequencyPenalty = (float) options.FrequencyPenalty,
        PresencePenalty = (float) options.PresencePenalty,
        TopP = (float) options.NucleusSampling
      }
    };
  }

  public int CountTokens(string text) => this._context.Tokenize(text, special: true).Length;

  public IReadOnlyList<string> GetTokens(string text)
  {
    LLamaToken[] source = this._context.Tokenize(text, special: true);
    StreamingTokenDecoder decoder = new StreamingTokenDecoder(this._context);
    Func<LLamaToken, string> selector = (Func<LLamaToken, string>) (x =>
    {
      decoder.Add(x);
      return decoder.Read();
    });
    return (IReadOnlyList<string>) ((IEnumerable<LLamaToken>) source).Select<LLamaToken, string>(selector).ToList<string>();
  }
}
