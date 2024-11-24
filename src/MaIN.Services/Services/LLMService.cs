using System.Collections.Concurrent;
using System.Text;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Sampling;
using LLama.Transformers;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using Microsoft.Extensions.Options;

public class LLMService(IOptions<MaINSettings> options) : ILLMService
{
    private static readonly ConcurrentDictionary<string, LLamaWeights> modelCache = new();
    private static readonly ConcurrentDictionary<string, ChatSession> sessionCache = new(); // Cache for chat sessions

    public async Task<ChatResult?> Send(Chat? chat, bool removeSession = false, bool temporaryChat = false)
    {
        var messageAlreadyAdded = false;
        var path = options.Value.ModelsPath;
        var model = KnownModels.GetModel(path, chat!.Model);
        var modelKey = model.FileName; // Use the model file name as a cache key.

        // Lazy load the model if it's not in the cache.
        var llmModel = await GetOrLoadModelAsync(path, modelKey);
        InferenceParams inferenceParams = new InferenceParams()
        {
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Temperature = 0.6f,
            },
            MaxTokens = 1024,
            AntiPrompts = [llmModel.Tokens.EndOfTurnToken ?? "User:"]
        };
        // Try to get a cached session for this model, create one if not found.
        if (!sessionCache.TryGetValue(chat.Id, out var cachedSession))
        {
            var parameters = new ModelParams(Path.Combine(path, modelKey))
            {
                ContextSize = 20000,
                GpuLayerCount = 40,
            };

            var context = llmModel.CreateContext(parameters);
            var historyTemp = new ChatHistory();
            var exTemp = new InteractiveExecutor(context);

            exTemp.AsChatClient().; //TODO
            // Create and cache the session.
            cachedSession = new ChatSession(exTemp, historyTemp);
            sessionCache[chat.Id] = cachedSession;
        }

        // Get the current session
        var session = cachedSession;
        
        // Add messages to history, skipping duplicates.
        foreach (var message in chat.Messages!.SkipLast(1))
        {
            if (session.History.Messages.Any(x => x.AuthorRole.ToString() == message.Role && x.Content == message.Content)) continue;
            session.History.AddMessage(Enum.Parse<AuthorRole>(message.Role), message.Content);
        }
        // Generate a response.
        Console.WriteLine("Generated Response:");
        session.WithHistoryTransform(new PromptTemplateTransformer(llmModel, withAssistant: true));

        // Add a transformer to eliminate printing the end of turn tokens
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            [llmModel.Tokens.EndOfTurnToken ?? "User:", "�"],
            redundancyLength: 5));

        var result = new StringBuilder();
        await foreach (var text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, chat.Messages!.Last().Content), inferenceParams))
        {
            Console.Write(text);
            result.Append(text);
        };

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

        if (removeSession)
        {
            sessionCache.Remove(chat.Id, out _);
        }
        
        return chatResult;
    }

    private async Task<LLamaWeights> GetOrLoadModelAsync(string path, string modelKey)
    {
        if (modelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = await LLamaWeights.LoadFromFileAsync(parameters);
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