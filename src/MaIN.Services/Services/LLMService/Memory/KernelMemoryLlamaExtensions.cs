using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
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
        out LLamaContext context)
    {
        context = model.CreateContext(modelParams);
        var executor = new BatchedExecutor(model, modelParams);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"],
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Grammar = memoryParams.Grammar != null ? new Grammar(memoryParams.Grammar, "root") : null
            }
        };

        builder.WithLLamaSharpTextGeneration(
            new LlamaSharpTextGen(
                model,
                context,
                executor, //TODO we should try to use batched executor so we can use conversation object and its state
                inferenceParams,
                memoryParams
            )
        );
        
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