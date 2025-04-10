using System.Collections.Concurrent;

using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Transformers;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
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
    
    private readonly MaINSettings _options;
    private readonly INotificationService _notificationService;
    private readonly IMemoryService _memoryService;
    private readonly IMemoryFactory _memoryFactory;
    private readonly string _modelsPath;

    public LLMService(
        MaINSettings options, 
        INotificationService notificationService,
        IMemoryService memoryService,
        IMemoryFactory memoryFactory)
    {
        _options = options;
        _notificationService = notificationService;
        _memoryService = memoryService;
        _memoryFactory = memoryFactory;
        _modelsPath = GetModelsPath();
    }
    
    public async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        if (!chat.Messages.Any())
            return null;

        var model = ModelHelper.GetModel(chat.Model);
        var modelKey = model.FileName;
        var thinkingState = new ThinkingState();

        var llmModel = await ModelLoader.GetOrLoadModelAsync(_modelsPath, modelKey);
        var session = GetOrCreateSession(chat.Id, () => CreateNewSession(llmModel, chat));
        var startSession = session.History.Messages.Count == 0;
        
        AddMessagesToHistory(session, chat.Messages);
        ConfigureSession(session, llmModel); //This makes significant performance difference

        var tokens = await ProcessChatRequest(
            chat, 
            session, 
            model, 
            llmModel, 
            startSession, 
            thinkingState,
            requestOptions, 
            cancellationToken);

        return CreateChatResult(chat, tokens, requestOptions);
    }
    
    public Task<string[]> GetCurrentModels()
    {
        var models = Directory.GetFiles(_modelsPath, "*.gguf", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(fileName => ModelHelper.GetModelByFileName(fileName) != null)
            .Select(fileName => ModelHelper.GetModelByFileName(fileName!).Name)
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
        var model = ModelHelper.GetModel(chat.Model);
        var kernelMemory = _memoryFactory.CreateMemoryWithParams(
            _modelsPath,
            model.FileName,
            chat.MemoryParams);

        await _memoryService.ImportDataToMemory(kernelMemory, memoryOptions, cancellationToken);

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
                Content = _memoryService.CleanResponseText(result.Result),
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    private string GetModelsPath()
    {
        var path = _options.ModelsPath ?? Environment.GetEnvironmentVariable(DEFAULT_MODEL_ENV_PATH);
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
        return new ModelParams(Path.Combine(_modelsPath, chat.Model))
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

    private ChatResult CreateChatResult(Chat chat, List<LLMTokenValue> tokens, ChatRequestOptions requestOptions)
    {
        var responseText = string.Concat(tokens.Select(x => x.Text));
        
        if (requestOptions.InteractiveUpdates)
        {
            SendNotification(chat.Id, new LLMTokenValue
            {
                Type = TokenType.FullAnswer,
                Text = responseText
            }, true).GetAwaiter().GetResult();
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
        await _notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateChatCompletion(chatId, token, isComplete), 
            "ReceiveMessageUpdate");
    }
}