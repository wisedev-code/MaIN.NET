using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions.Models;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using Grammar = LLama.Sampling.Grammar;
using InferenceParams = MaIN.Domain.Entities.InferenceParams;
#pragma warning disable KMEXP00

namespace MaIN.Services.Services.LLMService;

public class LLMService : ILLMService
{
    private const string DEFAULT_MODEL_ENV_PATH = "MaIN_ModelsPath";
    private static readonly ConcurrentDictionary<string, ChatSession> _sessionCache = new();
    private const int MaxToolIterations = 5;

    private readonly MaINSettings options;
    private readonly INotificationService notificationService;
    private readonly IMemoryService memoryService;
    private readonly IMemoryFactory memoryFactory;
    private readonly string modelsPath;

    private readonly JsonSerializerOptions _jsonToolOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

        var lastMsg = chat.Messages.Last();

        if (ChatHelper.HasFiles(lastMsg))
        {
            var memoryOptions = ChatHelper.ExtractMemoryOptions(lastMsg);
            return await AskMemory(chat, memoryOptions, requestOptions, cancellationToken);
        }

        if (chat.ToolsConfiguration?.Tools != null && chat.ToolsConfiguration.Tools.Any())
        {
            return await ProcessWithToolsAsync(chat, requestOptions, cancellationToken);
        }

        var model = KnownModels.GetModel(chat.Model);
        var tokens = await ProcessChatRequest(chat, model, lastMsg, requestOptions, cancellationToken);
        lastMsg.MarkProcessed();
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

        session.Executor.Context.Dispose();
        return Task.CompletedTask;
    }

    public async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        var model = KnownModels.GetModel(chat.Model);
        var finalModelPath = model.Path ?? modelsPath;
        var parameters = new ModelParams(Path.Combine(finalModelPath, model.FileName))
        {
            GpuLayerCount = chat.MemoryParams.GpuLayerCount,
            ContextSize = (uint)chat.MemoryParams.ContextSize,
        };
        var disableCache = chat.Properties.CheckProperty(ServiceConstants.Properties.DisableCacheProperty);
        var llmModel = disableCache
            ? await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken)
            : await ModelLoader.GetOrLoadModelAsync(finalModelPath, model.FileName);

        var memory = memoryFactory.CreateMemoryWithModel(
            finalModelPath,
            llmModel,
            model.FileName,
            chat.MemoryParams);

        await memoryService.ImportDataToMemory((memory.km, memory.generator), memoryOptions, cancellationToken);
        var userMessage = chat.Messages.Last();
        
        MemoryAnswer result;

        if (requestOptions.InteractiveUpdates || requestOptions.TokenCallback != null)
        {
            var responseBuilder = new StringBuilder();
        
            var searchOptions = new SearchOptions
            {
                Stream = true
            };

            await foreach (var chunk in memory.km.AskStreamingAsync(
                               userMessage.Content,
                               options: searchOptions,
                               cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Result))
                {
                    responseBuilder.Append(chunk.Result);
                    
                    var tokenValue = new LLMTokenValue
                    {
                        Text = chunk.Result,
                        Type = TokenType.Message
                    };
                    
                    if (requestOptions.InteractiveUpdates)
                    {
                        await notificationService.DispatchNotification(
                            NotificationMessageBuilder.CreateChatCompletion(chat.Id, tokenValue, false),
                            ServiceConstants.Notifications.ReceiveMessageUpdate);
                    }
                    
                    requestOptions.TokenCallback?.Invoke(tokenValue);
                }
            }
            
            result = new MemoryAnswer
            {
                Question = userMessage.Content,
                Result = responseBuilder.ToString(),
                NoResult = responseBuilder.Length == 0
            };
        }
        else
        {
            var searchOptions = new SearchOptions
            {
                Stream = false
            };

            result = await memory.km.AskAsync(
                userMessage.Content,
                options: searchOptions,
                cancellationToken: cancellationToken);
        }
        
        await memory.km.DeleteIndexAsync(cancellationToken: cancellationToken);
        
        if (disableCache)
        {
            llmModel.Dispose();
            ModelLoader.RemoveModel(model.FileName);
            memory.textGenerator.Dispose();
        }
        memory.generator._embedder.Dispose();
        memory.generator._embedder._weights.Dispose();
        memory.generator.Dispose();

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = memoryService.CleanResponseText(result.Result),
                Role = nameof(AuthorRole.Assistant),
                Type = MessageType.LocalLLM,
            }
        };
    }

    private async Task<List<LLMTokenValue>> ProcessChatRequest(
        Chat chat,
        Model model,
        Message lastMsg,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var modelKey = model.FileName;
        var thinkingState = new ThinkingState();
        var tokens = new List<LLMTokenValue>();

        var parameters = CreateModelParameters(chat, modelKey, model.Path);
        var disableCache = chat.Properties.CheckProperty(ServiceConstants.Properties.DisableCacheProperty);
        var llmModel = disableCache
            ? await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken)
            : await ModelLoader.GetOrLoadModelAsync(
                model.Path ?? modelsPath, modelKey);

        var llavaWeights = model.MMProject != null
            ? await LLavaWeights.LoadFromFileAsync(model.MMProject, cancellationToken)
            : null;
        
        using var executor = new BatchedExecutor(llmModel, parameters);

        var (conversation, isComplete, hasFailed) = await InitializeConversation(
            chat, lastMsg, model, llmModel, llavaWeights, executor, cancellationToken);

        if (!isComplete)
        {
            (tokens, isComplete, hasFailed) = await ProcessTokens(
                chat, conversation, model, llmModel, executor, thinkingState, requestOptions, cancellationToken);
        }

        if (isComplete && !hasFailed)
        {
            if (requestOptions.SaveConv)
            {
                chat.ConversationState = conversation.Save();
            }

            if (isComplete)
            {
                conversation.Dispose();
                if (disableCache)
                {
                    llmModel.Dispose();
                }
            }
        }

        return tokens;
    }

    private ModelParams CreateModelParameters(Chat chat, string modelKey, string? customPath)
    {
        return new ModelParams(Path.Combine(customPath ?? modelsPath, modelKey))
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

    private async Task<(Conversation Conversation, bool IsComplete, bool HasFailed)> InitializeConversation(Chat chat,
        Message lastMsg,
        Model model,
        LLamaWeights llmModel,
        LLavaWeights? llavaWeights,
        BatchedExecutor executor,
        CancellationToken cancellationToken)
    {
        var isNewConversation = chat.ConversationState == null;
        var conversation = isNewConversation
            ? executor.Create()
            : executor.Load(chat.ConversationState!);

        if (lastMsg.Image != null)
        {
            await ProcessImageMessage(conversation, lastMsg, llmModel, llavaWeights, executor, cancellationToken);
        }
        else
        {
            ProcessTextMessage(conversation, chat, lastMsg, model, llmModel, executor, isNewConversation);
        }

        return (conversation, false, false);
    }

    private static async Task ProcessImageMessage(Conversation conversation,
        Message lastMsg,
        LLamaWeights llmModel,
        LLavaWeights? llavaWeights,
        BatchedExecutor executor,
        CancellationToken cancellationToken)
    {
        var imageEmbeddings = llavaWeights?.CreateImageEmbeddings(lastMsg.Image!);
        conversation.Prompt(imageEmbeddings!);

        while (executor.BatchedTokenCount > 0)
            await executor.Infer(cancellationToken);

        var prompt = llmModel.Tokenize($"USER: {lastMsg.Content}\nASSISTANT:", true, false, Encoding.UTF8);
        conversation.Prompt(prompt);
    }

    private static void ProcessTextMessage(Conversation conversation,
        Chat chat,
        Message lastMsg,
        Model model,
        LLamaWeights llmModel,
        BatchedExecutor executor,
        bool isNewConversation)
    {
        var template = new LLamaTemplate(llmModel);
        var finalPrompt = ChatHelper.GetFinalPrompt(lastMsg, model, isNewConversation);

        var hasTools = chat.ToolsConfiguration?.Tools != null && chat.ToolsConfiguration.Tools.Any();

        if (isNewConversation)
        {
            var messagesToProcess = hasTools
                ? chat.Messages.SkipLast(1)
                : chat.Messages.Where(x => x.Properties.ContainsKey(Message.UnprocessedMessageProperty)).SkipLast(1);

            foreach (var messageToProcess in messagesToProcess)
            {
                template.Add(messageToProcess.Role, messageToProcess.Content);
            }
        }

        if (hasTools && isNewConversation)
        {
            var toolsPrompt = FormatToolsForPrompt(chat.ToolsConfiguration!);
            // Dodaj to jako wiadomoœæ systemow¹ lub na pocz¹tku pierwszego promptu u¿ytkownika
            finalPrompt = $"{toolsPrompt}\n\n{finalPrompt}";
        }

        template.Add(ServiceConstants.Roles.User, finalPrompt);
        template.AddAssistant = true;

        var templatedMessage = Encoding.UTF8.GetString(template.Apply());
        var tokens = isNewConversation
            ? executor.Context.Tokenize(templatedMessage, addBos: true, special: true)
            : executor.Context.Tokenize(templatedMessage);

        conversation.Prompt(tokens);
    }

    private static string FormatToolsForPrompt(ToolsConfiguration toolsConfig)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## TOOLS");
        sb.AppendLine("You can call these tools if needed. To call a tool, respond with a JSON object inside <tool_call> tags.");

        foreach (var tool in toolsConfig.Tools)
        {
            // TODO: refactor to not allow null Function
            sb.AppendLine($"- {tool.Function.Name}: {tool.Function.Description}");
            sb.AppendLine($"  Parameters: {JsonSerializer.Serialize(tool.Function.Parameters)}");
        }

        sb.AppendLine("\n## RESPONSE FORMAT (YOU HAVE TO CHOOSE ONE FORMAT AND CANNOT MIX THEM)##");
        sb.AppendLine("1. For normal conversation, just respond with plain text.");
        sb.AppendLine("2. For tool calls, use this format. " +
            "You cannot respond with plain text before or after format. " +
            "If you want to call multiple functions, you have to combine them into one array." +
            "Your response MUST contain only one tool call block:");
        sb.AppendLine("<tool_call>");
        sb.AppendLine("{\"tool_calls\": [{\"id\": \"call_1\", \"type\": \"function\", \"function\": {\"name\": \"tool_name\", \"arguments\": \"{\\\"param\\\":\\\"value\\\"}\"}},{\"id\": \"call_2\", \"type\": \"function\", \"function\": {\"name\": \"tool2_name\", \"arguments\": \"{\\\"param1\\\":\\\"value1\\\",\\\"param2\\\":\\\"value2\\\"}\"}}]}");
        sb.AppendLine("</tool_call>");

        return sb.ToString();
    }

    private List<ToolCall>? ParseToolCalls(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;

        string jsonContent = ExtractJsonContent(response);
        try
        {
            if (string.IsNullOrEmpty(jsonContent)) return null;

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // OpenAI standard { "tool_calls": [...] }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tool_calls", out var toolCallsProp))
            {
                var calls = toolCallsProp.Deserialize<List<ToolCall>>(_jsonToolOptions);
                return NormalizeToolCalls(calls);
            }

            // TODO: test if those formats are used by any model
            // model returned table [ { ... }, { ... } ]
            if (root.ValueKind == JsonValueKind.Array)
            {
                var calls = root.Deserialize<List<ToolCall>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return NormalizeToolCalls(calls);
            }

            // flat format { "tool_name": "...", "arguments": {...} }
            if (root.ValueKind == JsonValueKind.Object && (root.TryGetProperty("tool_name", out _) || root.TryGetProperty("function", out _)))
            {
                var singleCall = ParseSingleLegacyCall(root);
                if (singleCall != null) return new List<ToolCall> { singleCall };
            }
        }
        catch (Exception)
        {
            // No tool calls found no need to throw nor log
        }

        return null;
    }

    private string ExtractJsonContent(string text)
    {
        text = text.Trim();

        int firstBrace = text.IndexOf('{');
        int firstBracket = text.IndexOf('[');
            int startIndex = (firstBrace >= 0 && firstBracket >= 0) ? Math.Min(firstBrace, firstBracket) : Math.Max(firstBrace, firstBracket);

        int lastBrace = text.LastIndexOf('}');
        int lastBracket = text.LastIndexOf(']');
            int endIndex = Math.Max(lastBrace, lastBracket);

            if (startIndex >= 0 && endIndex > startIndex)
            {
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }

        return text;
    }

    private ToolCall? ParseSingleLegacyCall(JsonElement root)
    {
        string name = string.Empty;
        if (root.TryGetProperty("tool_name", out var tn)) name = tn.GetString();
        else if (root.TryGetProperty("function", out var fn) && fn.ValueKind == JsonValueKind.String) name = fn.GetString();
        else if (root.TryGetProperty("function", out var fnObj) && fnObj.TryGetProperty("name", out var n)) name = n.GetString();

        if (string.IsNullOrEmpty(name)) return null;

        string? args = "{}";
        if (root.TryGetProperty("arguments", out var argProp))
        {
            args = argProp.ValueKind == JsonValueKind.String ? argProp.GetString() : argProp.GetRawText();
        }
        else if (root.TryGetProperty("parameters", out var paramProp))
        {
            args = paramProp.GetRawText();
        }

        return new ToolCall
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Type = "function",
            Function = new FunctionCall { Name = name, Arguments = args! }
        };
    }

    private List<ToolCall> NormalizeToolCalls(List<ToolCall>? calls)
    {
        if (calls == null) return new List<ToolCall>();
        foreach (var call in calls)
        {
            if (string.IsNullOrEmpty(call.Id)) call.Id = Guid.NewGuid().ToString().Substring(0, 8);
            if (string.IsNullOrEmpty(call.Type)) call.Type = "function";
            if (call.Function == null) call.Function = new FunctionCall();
        }
        return calls;
    }

    public class ToolCall
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [System.Text.Json.Serialization.JsonPropertyName("function")]
        public FunctionCall Function { get; set; } = new();
    }

    public class FunctionCall
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("arguments")]
        public string Arguments { get; set; } = "{}";
    }

    private async Task<(List<LLMTokenValue> Tokens, bool IsComplete, bool HasFailed)> ProcessTokens(
        Chat chat,
        Conversation conversation,
        Model model,
        LLamaWeights llmModel,
        BatchedExecutor executor,
        ThinkingState thinkingState,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var tokens = new List<LLMTokenValue>();
        var isComplete = false;
        var hasFailed = false;

        using var sampler = CreateSampler(chat.InterferenceParams);
        var decoder = new StreamingTokenDecoder(executor.Context);

        var inferenceParams = ChatHelper.CreateInferenceParams(chat, llmModel);
        var maxTokens = inferenceParams.MaxTokens == -1 ? int.MaxValue : inferenceParams.MaxTokens;

        for (var i = 0; i < maxTokens && !isComplete; i++)
        {
            var decodeResult = await executor.Infer(cancellationToken);

            if (decodeResult == DecodeResult.NoKvSlot)
            {
                isComplete = true;
                hasFailed = true;
                chat.ConversationState = null;
                break;
            }

            if (decodeResult == DecodeResult.DecodeFailed)
                throw new Exception("Unknown error occurred while inferring.");

            if (!conversation.RequiresSampling)
                continue;

            var token = conversation.Sample(sampler);
            var vocab = executor.Context.NativeHandle.ModelHandle.Vocab;

            if (token.IsEndOfGeneration(vocab))
            {
                isComplete = true;
            }
            else
            {
                decoder.Add(token);
                var tokenTxt = decoder.Read();

                conversation.Prompt(token);
                var tokenValue = model.ReasonFunction != null
                    ? model.ReasonFunction(tokenTxt, thinkingState)
                    : new LLMTokenValue()
                    {
                        Text = tokenTxt,
                        Type = TokenType.Message
                    };

                tokens.Add(tokenValue);

                if (requestOptions.InteractiveUpdates)
                {
                    await SendNotification(chat.Id, tokenValue, false);
                }

                requestOptions.TokenCallback?.Invoke(tokenValue);
            }
        }
        

        return (tokens, isComplete, hasFailed);
    }

    private BaseSamplingPipeline CreateSampler(InferenceParams interferenceParams)
    {
        if (interferenceParams.Temperature == 0)
        {
            return new GreedySamplingPipeline()
            {
                Grammar = interferenceParams.Grammar != null ? new Grammar(interferenceParams.Grammar.Value, "root") : null
            };
        }

        return new DefaultSamplingPipeline()
        {
            Temperature = interferenceParams.Temperature,
            TopP = interferenceParams.TopP,
            TopK = interferenceParams.TopK,
            Grammar = interferenceParams.Grammar != null ? new Grammar(interferenceParams.Grammar.Value, "root") : null
        };
    }


    private string GetModelsPath()
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DEFAULT_MODEL_ENV_PATH);
        return string.IsNullOrEmpty(path) 
            ? throw new ModelsPathNotFoundException() 
            : path;
    }

    private async Task<ChatResult> CreateChatResult(Chat chat, List<LLMTokenValue> tokens,
        ChatRequestOptions requestOptions)
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
                Role = AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM,
            }.MarkProcessed()
        };
    }

    private async Task SendNotification(string chatId, LLMTokenValue token, bool isComplete)
    {
        await notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateChatCompletion(chatId, token, isComplete),
            ServiceConstants.Notifications.ReceiveMessageUpdate);
    }

    private async Task<ChatResult> ProcessWithToolsAsync(
        Chat chat,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        NativeLogConfig.llama_log_set((level, message) => {
            if (level == LLamaLogLevel.Error)
            {
                Console.Error.Write(message);
            }
        }); // Remove llama native logging

        var model = KnownModels.GetModel(chat.Model);
        var tokens = new List<LLMTokenValue>();
        var fullResponseBuilder = new StringBuilder();
        var iterations = 0;

        while (iterations < MaxToolIterations)
        {
            var lastMsg = chat.Messages.Last();
            await SendNotification(chat.Id, new LLMTokenValue
            {
                Type = TokenType.FullAnswer,
                Text = $"Processing with tools... iteration {iterations + 1}\n\n"
            }, false);
            requestOptions.InteractiveUpdates = false;
            var iterationTokens = await ProcessChatRequest(chat, model, lastMsg, requestOptions, cancellationToken);
            
            var responseText = string.Concat(iterationTokens.Select(x => x.Text));
            
            if (fullResponseBuilder.Length > 0)
            {
                fullResponseBuilder.Append('\n');
            }
            fullResponseBuilder.Append(responseText);
            tokens.AddRange(iterationTokens);

            var toolCalls = ParseToolCalls(responseText);

            if (toolCalls == null || !toolCalls.Any())
            {
                requestOptions.InteractiveUpdates = true;
                await SendNotification(chat.Id, new LLMTokenValue
                {
                    Type = TokenType.FullAnswer,
                    Text = responseText
                }, false);
                break;
            }

            var assistantMessage = new Message
            {
                Content = responseText,
                Role = AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM,
                Tool = true
            };
            assistantMessage.Properties[ToolCallsProperty] = JsonSerializer.Serialize(toolCalls);
            chat.Messages.Add(assistantMessage.MarkProcessed());

            foreach (var toolCall in toolCalls)
            {                
                if (chat.Properties.CheckProperty(ServiceConstants.Properties.AgentIdProperty))
                {
                    await notificationService.DispatchNotification(
                        NotificationMessageBuilder.ProcessingTools(
                            chat.Properties[ServiceConstants.Properties.AgentIdProperty],
                            string.Empty,
                            toolCall.Function.Name),
                        ServiceConstants.Notifications.ReceiveAgentUpdate);
                }

                var executor = chat.ToolsConfiguration?.GetExecutor(toolCall.Function.Name);

                if (executor == null)
                {
                    var errorMessage = $"No executor found for tool: {toolCall.Function.Name}";
                    throw new InvalidOperationException(errorMessage);
                }


                try
                {
                    requestOptions.ToolCallback?.Invoke(new ToolInvocation
                    {
                        ToolName = toolCall.Function.Name,
                        Arguments = toolCall.Function.Arguments,
                        Done = false
                    });

                    var toolResult = await executor(toolCall.Function.Arguments);

                    requestOptions.ToolCallback?.Invoke(new ToolInvocation
                    {
                        ToolName = toolCall.Function.Name,
                        Arguments = toolCall.Function.Arguments,
                        Done = true
                    });

                    var toolMessage = new Message
                    {
                        Content = $"Tool result for {toolCall.Function.Name}: {toolResult}",
                        Role = ServiceConstants.Roles.Tool,
                        Type = MessageType.LocalLLM,
                        Tool = true
                    };
                    toolMessage.Properties[ToolCallIdProperty] = toolCall.Id;
                    toolMessage.Properties[ToolNameProperty] = toolCall.Function.Name;
                    chat.Messages.Add(toolMessage.MarkProcessed());
                }
                catch (Exception ex)
                {
                    var errorResult = JsonSerializer.Serialize(new { error = ex.Message });
                    var toolMessage = new Message
                    {
                        Content = $"Tool error for {toolCall.Function.Name}: {errorResult}",
                        Role = ServiceConstants.Roles.Tool,
                        Type = MessageType.LocalLLM,
                        Tool = true
                    };
                    toolMessage.Properties[ToolCallIdProperty] = toolCall.Id;
                    toolMessage.Properties[ToolNameProperty] = toolCall.Function.Name;
                    chat.Messages.Add(toolMessage.MarkProcessed());
                }
            }

            iterations++;
        }

        if (iterations >= MaxToolIterations)
        {
            await SendNotification(chat.Id, new LLMTokenValue
            {
                Type = TokenType.FullAnswer,
                Text = "Maximum tool invocation iterations reached. Ending the conversation."
            }, false);
        }

        var finalResponse = fullResponseBuilder.ToString();
        chat.Messages.Last().MarkProcessed();

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = finalResponse,
                Tokens = tokens,
                Role = AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM,
            }.MarkProcessed()
        };
    }

    private const string ToolCallsProperty = "ToolCalls";
    private const string ToolCallIdProperty = "ToolCallId";
    private const string ToolNameProperty = "ToolName";
}
