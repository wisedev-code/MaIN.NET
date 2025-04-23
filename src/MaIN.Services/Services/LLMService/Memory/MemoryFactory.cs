using LLama;
using LLama.Common;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.LLMService.Utils;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public class MemoryFactory(MaINSettings settings) : IMemoryFactory
{
    public IKernelMemory CreateMemory(string modelsPath, string modelName)
    {
        var model = ModelLoader.GetOrLoadModel(modelsPath, modelName);
        return CreateMemoryWithModel(modelsPath, model, new MemoryParams
        {
            MaxMatchesCount = 5,
            FrequencyPenalty = 1,
            Temperature = 0.6f,
            AnswerTokens = 1024
        });
    }

    public IKernelMemory CreateMemoryWithModel(string modelsPath,
        LLamaWeights model,
        MemoryParams memoryParams)
    {
        var path = ResolvePath(modelsPath);
        var embeddingModel = KnownModels.GetEmbeddingModel();
        var embeddingModelPath = Path.Combine(path, embeddingModel.FileName);

        var generator = ConfigureGeneratorOptions(embeddingModelPath);
        var searchOptions = ConfigureSearchOptions(memoryParams);
        var parsingOptions = ConfigureParsingOptions();
        var modelParams = new ModelParams(modelsPath)
        {
            ContextSize = (uint)memoryParams.ContextSize,
            GpuLayerCount = memoryParams.GpuLayerCount,
        };
            
        return new KernelMemoryBuilder()
            .WithLLamaSharpTextGeneration(model, modelParams)
            .WithLLamaSharpTextEmbeddingGeneration(generator)
            .WithSearchClientConfig(searchOptions)
            .WithCustomImageOcr(new OcrWrapper())
            .With(parsingOptions)
            .Build();
    }
    
    public IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams)
    {
        var searchOptions = ConfigureSearchOptions(memoryParams);

        var kernelMemory = new KernelMemoryBuilder()
            .WithSearchClientConfig(searchOptions)
            .WithOpenAIDefaults(openAiKey)
            .Build();
        
        return kernelMemory;
    }
    
    #region Private Configuration Methods

    private string ResolvePath(string modelsPath)
    {
        var path = modelsPath ?? settings.ModelsPath ?? 
            Environment.GetEnvironmentVariable("MaIN_ModelsPath");
            
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Models path not found");
        }
        
        return path;
    }
    
    private static LLamaSharpTextEmbeddingGenerator ConfigureGeneratorOptions(string embeddingModelPath)
    {
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"] 
        };

        var config = new LLamaSharpConfig(embeddingModelPath)
        { 
            DefaultInferenceParams = inferenceParams,
            GpuLayerCount = 20,
        };
        
        var parameters = new ModelParams(config.ModelPath)
        {
            ContextSize = new uint?(config.ContextSize.GetValueOrDefault(2048U)),
            GpuLayerCount = config.GpuLayerCount.GetValueOrDefault(20),
        };
        
        var weights = LLamaWeights.LoadFromFile(parameters);

        return new LLamaSharpTextEmbeddingGenerator(config, weights);
    }
    
    private static SearchClientConfig ConfigureSearchOptions(MemoryParams memoryParams)
    {
        return new SearchClientConfig
        {
            MaxMatchesCount = memoryParams.MaxMatchesCount,
            FrequencyPenalty = memoryParams.FrequencyPenalty,
            Temperature = memoryParams.Temperature,
            AnswerTokens = memoryParams.AnswerTokens,
        };
    }
    
    private static TextPartitioningOptions ConfigureParsingOptions()
    {
        return new TextPartitioningOptions
        {
            MaxTokensPerParagraph = 300,
        };
    }
    
    #endregion
}