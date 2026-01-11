using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using MaIN.Services.Services.LLMService.Memory.Embeddings;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using InferenceParams = LLama.Common.InferenceParams;
#pragma warning disable KMEXP00

namespace MaIN.Services.Services.LLMService.Memory;

public static class KernelMemoryLlamaExtensions 
{
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LLamaWeights model, 
        ModelParams modelParams,
        MemoryParams memoryParams,
        out LlamaSharpTextGen textGen)
    {
        var context = model.CreateContext(modelParams);
        var executor = new BatchedExecutor(model, modelParams);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:", "Answer:", "INFO NOT FOUND"],
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Grammar = memoryParams.Grammar != null ? new Grammar(memoryParams.Grammar.Value, "root") : null
            }
        };
        textGen = new LlamaSharpTextGen(
            model,
            context,
            executor,
            inferenceParams,
            memoryParams
        );
        
        builder.WithLLamaSharpTextGeneration(textGen);
        return builder;
    }
    
    
    public static IKernelMemoryBuilder WithLLamaSharpTextEmbeddingOwnGeneration(
        this IKernelMemoryBuilder builder,
        LLamaSharpTextEmbeddingMaINClone textEmbeddingGenerator)
    {
        builder.AddSingleton((ITextEmbeddingGenerator) textEmbeddingGenerator);
        builder.AddIngestionEmbeddingGenerator(textEmbeddingGenerator);
        return builder;
    }
    
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpTextGen textGenerator)
    {
        builder.AddSingleton<ITextGenerator>((ITextGenerator) textGenerator);
        return builder;
    }
}