using System.Collections.Concurrent;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLama.Transformers;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Utils;
using Microsoft.Extensions.Options;
using ChatHistory = LLama.Common.ChatHistory;

namespace MaIN.Services.Services;

public class LLMService(IOptions<MaINSettings> options) : ILLMService
{
    private static readonly ConcurrentDictionary<string, LLamaWeights> modelCache = new();

    public async Task<ChatResult?> Send(Chat? chat)
    {
        var path = options.Value.ModelsPath;
        var model = KnownModels.GetModel(path, chat!.Model);

        var modelKey = model.FileName; // Use the model file name as a cache key.

        // Lazy load the model if it's not in the cache.
        var llmModel = await GetOrLoadModelAsync(path, modelKey);
        // Set up execution parameters.
        var parameters = new ModelParams(Path.Combine(path, modelKey))
        {
            ContextSize = 20000,
            GpuLayerCount = 250,
            
        };

        using var context = llmModel.CreateContext(parameters);
        var history = new ChatHistory();
        var ex = new InteractiveExecutor(context);
     //   var completion = new LLamaSharpChatCompletion(ex);
        
        // Add messages to history, skipping duplicates.
        foreach (var message in chat.Messages!.SkipLast(1))
        {
            if (history.Messages.Any(x => x.AuthorRole.ToString() == message.Role && x.Content == message.Content)) continue;
            history.AddMessage(Enum.Parse<AuthorRole>(message.Role), message.Content);
        }

        InferenceParams inferenceParams = new InferenceParams()
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.6f
            },
            MaxTokens = -1, // keep generating tokens until the anti prompt is encountered
            AntiPrompts = [llmModel.Tokens.EndOfTurnToken ?? "User:"] // model specific end of turn string (or default)
        };
        
        // Generate a response.
        Console.WriteLine("Generated Response:");
        ChatSession session = new(ex, history);
        session.WithHistoryTransform(new PromptTemplateTransformer(llmModel, withAssistant: true)); 

        // Add a transformer to eliminate printing the end of turn tokens, llama 3 specifically has an odd LF that gets printed sometimes
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            [llmModel.Tokens.EndOfTurnToken ?? "User:", "�"],
            redundancyLength: 5));

        var result = new StringBuilder();
        await foreach ( // Generate the response streamingly.
                       var text
                       in session.ChatAsync(
                           new ChatHistory.Message(AuthorRole.User, chat.Messages!.Last().Content),
                           inferenceParams))
        {
            Console.Write(text);
            result.Append(text);
        };
        
        // var finalResult = result.ToString().EndsWith("User:") 
        //     ? result.ToString()[..^5] 
        //     : result.ToString();
        //
        var finalResult = result.Replace("Assistant:", string.Empty)
            .Replace("Response:", string.Empty)
            .Replace("<|endoftext|>", string.Empty)
            .Replace("<|im_end|>", string.Empty)
            .Replace("<|im_start|>", string.Empty)
            .Replace("Ċ", string.Empty) //dont ask why
            .ToString(); 
        
        finalResult = finalResult.StartsWith("assistant:") ? finalResult[8..] : finalResult;
        
        var chatResult = new ChatResult()
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto()
            {
                Content = result.ToString(),
                Role = AuthorRole.Assistant.ToString()
            }
        };

        return chatResult;
    }

// Method to get or load model asynchronously.
    private async Task<LLamaWeights> GetOrLoadModelAsync(string path, string modelKey)
    {
        // If the model is already loaded in the cache, return it.
        if (modelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        // Load the model and cache it.
        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = await LLamaWeights.LoadFromFileAsync(parameters);

        // Cache the model if it's not already present, ensuring thread safety.
        return modelCache.GetOrAdd(modelKey, loadedModel);
    }

    public Task<List<string>> GetCurrentModels()
    {
        var path = options.Value.ModelsPath;
        var files = Directory.GetFiles(path, "*.gguf", SearchOption.AllDirectories).ToList();
        var models = new List<string>();
        foreach (var file in files)
        {
            var model = KnownModels.GetModelByFileName(path, Path.GetFileName(file));
            if (model != null)
            {
                models.Add(model.Value.Name);
            }
        }

        return Task.FromResult(models);
    }
}