using System.Collections.Concurrent;

using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Transformers;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService;

public class LLMService : ILLMService
{
    private const string DEFAULT_MODEL_ENV_PATH = "MaIN_ModelsPath";
    private static readonly ConcurrentDictionary<string, ChatSession> _sessionCache = new();
    
    private readonly MaINSettings options;
    private readonly INotificationService notificationService;
    private readonly IMemoryService memoryService;
    private readonly IMemoryFactory memoryFactory;
    private readonly string modelsPath;

    public LLMService(
        MaINSettings options, 
        INotificationService notificationService,
        IMemoryService memoryService,
        IMemoryFactory memoryFactory)
    {
        this.options = options;
        this.notificationService = notificationService;
        this.memoryService = memoryService;
        this.memoryFactory = memoryFactory;
        modelsPath = GetModelsPath();
    }
    
    public async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        if (chat.Messages.Count == 0)
            return null;

        var model = KnownModels.GetModel(chat.Model);
        var modelKey = model.FileName;
        var thinkingState = new ThinkingState();

        var llmModel = await ModelLoader.GetOrLoadModelAsync(modelsPath, modelKey);
        var session = GetOrCreateSession(chat.Id, () => CreateNewSession(llmModel, chat));
        var startSession = session.History.Messages.Count == 0;
        
        AddMessagesToHistory(session, chat.Messages);
        ConfigureSession(session, llmModel); //This makes significant performance difference TBD: validate

        var tokens = await ProcessChatRequest(
            chat, 
            session, 
            model, 
            llmModel, 
            startSession, 
            thinkingState,
            requestOptions, 
            cancellationToken);

        return await CreateChatResult(chat, tokens, requestOptions);
    }
    
    public Task<string[]> GetCurrentModels()
    {
        var models = Directory.GetFiles(modelsPath, "*.gguf", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(fileName => KnownModels.GetModelByFileName(modelsPath, fileName!) != null)
            .Select(fileName => KnownModels.GetModelByFileName(modelsPath, fileName!)!.Name)
            .ToArray();

        return Task.FromResult(models);
    }
    
    public Task CleanSessionCache(string? id)
    {
        if (string.IsNullOrEmpty(id) || !_sessionCache.TryRemove(id, out var session))
            return Task.CompletedTask;
            
        session.Executor.Context.NativeHandle.KvCacheClear();
        session.Executor.Context.Dispose();
        return Task.CompletedTask;
    }
    
    public async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        var model = KnownModels.GetModel(chat.Model);
        var parameters = new ModelParams(Path.Combine(modelsPath, model.FileName))
        {
            GpuLayerCount = chat.MemoryParams.GpuLayerCount,
            ContextSize = (uint)chat.MemoryParams.ContextSize
        };
        var llmModel = await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken);
        var kernelMemory = memoryFactory.CreateMemoryWithModel(
            modelsPath,
            llmModel,
            chat.MemoryParams);

        await memoryService.ImportDataToMemory(kernelMemory, memoryOptions, cancellationToken);
        var userMessage = chat.Messages.Last();
        var result = await kernelMemory.AskAsync(userMessage.Content, cancellationToken: cancellationToken);
        await kernelMemory.DeleteIndexAsync(cancellationToken: cancellationToken);

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = memoryService.CleanResponseText(result.Result),
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    private string GetModelsPath()
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DEFAULT_MODEL_ENV_PATH);
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Models path not found in configuration or environment variables");
        }
        return path;
    }

    private ChatSession GetOrCreateSession(string chatId, Func<ChatSession> createSession)
    {
        return _sessionCache.GetOrAdd(chatId, _ => createSession());
    }

    private ChatSession CreateNewSession(LLamaWeights model, Chat chat)
    {
        var parameters = CreateModelParameters(model, chat);
        var context = model.CreateContext(parameters);
        var history = new ChatHistory();
        var executor = new InteractiveExecutor(context);
        return new ChatSession(executor, history);
    }

    private ModelParams CreateModelParameters(LLamaWeights model, Chat chat)
    {
        return new ModelParams(Path.Combine(modelsPath, chat.Model))
        {
            ContextSize = (uint?)chat.InterferenceParams.ContextSize,
            GpuLayerCount = chat.InterferenceParams.GpuLayerCount,
            SeqMax = chat.InterferenceParams.SeqMax,
            BatchSize = chat.InterferenceParams.BatchSize,
            UBatchSize = chat.InterferenceParams.UBatchSize,
            Embeddings = chat.InterferenceParams.Embeddings,
            TypeK = (GGMLType)chat.InterferenceParams.TypeK,
            TypeV = (GGMLType)chat.InterferenceParams.TypeV,
        };
    }

    private void ConfigureSession(ChatSession session, LLamaWeights model)
    {
        session.WithHistoryTransform(new PromptTemplateTransformer(model, withAssistant: true));
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            [model.Vocab.EOT.ToString() ?? "User:", "�", "Assistant:"],
            redundancyLength: 5));
    }

    private void AddMessagesToHistory(ChatSession session, List<Message> messages)
    {
        var existingMessages = session.History.Messages
            .Select(m => new { m.AuthorRole, m.Content })
            .ToHashSet();

        foreach (var message in messages.SkipLast(1))
        {
            var messageKey = new { AuthorRole = Enum.Parse<AuthorRole>(message.Role), message.Content };

            if (!existingMessages.Contains(messageKey))
            {
                session.History.AddMessage(messageKey.AuthorRole, message.Content);
            }
        }
    }

    private async Task<List<LLMTokenValue>> ProcessChatRequest(
        Chat chat,
        ChatSession session,
        Model model,
        LLamaWeights llmModel, 
        bool startSession,
        ThinkingState thinkingState,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var tokens = new List<LLMTokenValue>();
        var lastMessage = chat.Messages.Last();
        
        if (ChatHelper.HasFiles(lastMessage))
        {
            var memoryOptions = ChatHelper.ExtractMemoryOptions(lastMessage);
            var result = await AskMemory(chat, memoryOptions, cancellationToken);
            
            if (result?.Message.Content != null)
            {
                tokens.Add(new LLMTokenValue
                {
                    Type = TokenType.FullAnswer,
                    Text = result.Message.Content
                });
            }
            
            return tokens;
        }
        
        var finalPrompt = ChatHelper.GetFinalPrompt(lastMessage, model, startSession);
        var inferenceParams = ChatHelper.CreateInferenceParams(chat, llmModel);

        await foreach (var text in session.ChatAsync(
                        new ChatHistory.Message(AuthorRole.User, finalPrompt),
                        inferenceParams, 
                        cancellationToken))
        {
            var token = model.ReasonFunction != null
                ? model.ReasonFunction(text, thinkingState)
                : new LLMTokenValue
                {
                    Type = TokenType.Message,
                    Text = text
                };
            
            if (requestOptions.InteractiveUpdates)
            {
                await SendNotification(chat.Id, token, false);
            }

            requestOptions.TokenCallback?.Invoke(token);
            tokens.Add(token);
        }
        
        return tokens;
    }

    private async Task<ChatResult> CreateChatResult(Chat chat, List<LLMTokenValue> tokens, ChatRequestOptions requestOptions)
    {
        var responseText = string.Concat(tokens.Select(x => x.Text));
        
        if (requestOptions.InteractiveUpdates)
        {
            await SendNotification(chat.Id, new LLMTokenValue
            {
                Type = TokenType.FullAnswer,
                Text = responseText
            }, true);
        }

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = responseText,
                Tokens = tokens,
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    private async Task SendNotification(string chatId, LLMTokenValue token, bool isComplete)
    {
        await notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateChatCompletion(chatId, token, isComplete), 
            ServiceConstants.Notifications.ReceiveMessageUpdate);
    }
}