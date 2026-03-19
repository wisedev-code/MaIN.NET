using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;
using MaIN.Services.Utils;
using System.Text.Json;

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler(
    ILLMServiceFactory llmServiceFactory,
    IMcpService mcpService,
    INotificationService notificationService,
    IImageGenServiceFactory imageGenServiceFactory)
    : ICommandHandler<AnswerCommand, Message?>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Message?> HandleAsync(AnswerCommand command)
    {
        if (!ModelRegistry.TryGetById(command.Chat.ModelId, out var model))
        {
            throw new AgentModelNotAvailableException(command.AgentId, command.Chat.ModelId);
        }

        ChatResult? result;
        var backend = model!.Backend;
        var llmService = llmServiceFactory.CreateService(backend);
        var imageGenService = imageGenServiceFactory.CreateService(backend);

        switch (command.KnowledgeUsage)
        {
            case KnowledgeUsage.UseMemory:
                result = await llmService.AskMemory(command.Chat,
                    new ChatMemoryOptions { Memory = command.Chat.Memory }, new ChatRequestOptions());
                return result!.Message;
            case KnowledgeUsage.UseKnowledge:
                var isKnowledgeNeeded = await ShouldUseKnowledge(command.Knowledge, command.Chat, backend);
                if (isKnowledgeNeeded)
                {
                    return await ProcessKnowledgeQuery(command.Knowledge, command.Chat, command.AgentId, llmService);
                }

                break;
            case KnowledgeUsage.AlwaysUseKnowledge:
                return await ProcessKnowledgeQuery(command.Knowledge, command.Chat, command.AgentId, llmService);
        }

        result = command.Chat.ImageGen
            ? await imageGenService!.Send(command.Chat)
            : await llmService.Send(command.Chat,
                new ChatRequestOptions
                {
                    InteractiveUpdates = command.Chat.Interactive,
                    TokenCallback = command.Callback,
                    ToolCallback = command.ToolCallback
                });

        return result!.Message;
    }

    private async Task<bool> ShouldUseKnowledge(Knowledge? knowledge, Chat chat, BackendType backend)
    {
        var originalContent = chat.Messages.Last().Content;

        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, _jsonOptions);

        chat.InferenceGrammar = new Grammar(ServiceConstants.Grammars.DecisionGrammar, GrammarFormat.GBNF);
        chat.Messages.Last().Content =
            $"""
             KNOWLEDGE:
             {index}

                   Based on the following prompt, decide if you should use external knowledge.
                   Use external knowledge unless you're certain the question requires only basic facts (like "What is 2+2?" or "Capital of France?").
                   When in doubt, use it - external knowledge often provides more current, specific, or contextual information.
                   Content of available knowledge has source tags. Prompt: {originalContent}
             """;

        var service = llmServiceFactory.CreateService(backend);

        var result = await service.Send(chat, new ChatRequestOptions()
        {
            SaveConv = false
        });
        var decision = JsonSerializer.Deserialize<JsonElement>(result!.Message.Content, _jsonOptions);
        var decisionValue = decision.GetProperty("decision").GetRawText();
        chat.InferenceGrammar = null;
        var shouldUseKnowledge = bool.Parse(decisionValue.Trim('"'));
        chat.Messages.Last().Content = originalContent;
        return shouldUseKnowledge;
    }

    private async Task<Message?> ProcessKnowledgeQuery(Knowledge? knowledge, Chat chat, string agentId, ILLMService llmService)
    {
        var originalContent = chat.Messages.Last().Content;
        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, _jsonOptions);

        chat.InferenceGrammar = new Grammar(ServiceConstants.Grammars.KnowledgeGrammar, GrammarFormat.GBNF);
        chat.Messages.Last().Content =
            $"""
             KNOWLEDGE:
             {index}

             Find tags that fits user query based on available knowledge (provided to you above as pair of item names with tags).
             Always return at least 1 tag in array, and no more than 4. Prompt: {originalContent}
             """;

        var searchResult = await llmService.Send(chat, new ChatRequestOptions()
        {
            SaveConv = false
        });
        var matchedTags = JsonSerializer.Deserialize<List<string>>(searchResult!.Message.Content, _jsonOptions);
        var knowledgeItems = knowledge!.Index.Items
            .Where(x => x.Tags.Intersect(matchedTags!).Any() || matchedTags!.Contains(x.Name))
            .ToList();

        //NOTE: perhaps good idea for future to combine knowledge form MCP and from KM
        var memoryOptions = new ChatMemoryOptions();
        var mcpConfig = BuildMemoryOptionsFromKnowledgeItems(knowledgeItems, memoryOptions);

        chat.Messages.Last().Content = $"{originalContent} - Use information given you as memory.";
        chat.MemoryParams.IncludeQuestionSource = true;
        chat.MemoryParams.Grammar = null;

        await notificationService.DispatchNotification(NotificationMessageBuilder.CreateActorKnowledgeStepProgress(
            agentId,
            [.. knowledgeItems.Select(x => $" {x.Name}|{x.Type} ")],
            mcpConfig?.Model ?? chat.ModelId), "ReceiveAgentUpdate");

        if (mcpConfig is not null)
        {
            var result = await mcpService.Prompt(mcpConfig, chat.Messages);
            return result.Message;
        }

        var knowledgeResult = await llmService.AskMemory(chat, memoryOptions, new ChatRequestOptions());
        chat.Messages.Last().Content = originalContent;
        return knowledgeResult?.Message;
    }

    private static Mcp? BuildMemoryOptionsFromKnowledgeItems(List<KnowledgeIndexItem>? knowledgeItems,
        ChatMemoryOptions memoryOptions)
    {
        //First or default because we cannot combine response from multiple servers in one go at the moment
        var mcp = knowledgeItems?.FirstOrDefault(x => x.Type == KnowledgeItemType.Mcp);
        if (mcp is not null)
        {
            return JsonSerializer.Deserialize<Mcp>(mcp.Value, _jsonOptions);
        }

        foreach (var item in knowledgeItems!)
        {
            switch (item.Type)
            {
                case KnowledgeItemType.File:
                    memoryOptions.FilesData.TryAdd(item.Name, item.Value);
                    break;
                case KnowledgeItemType.Text:
                    memoryOptions.TextData.TryAdd(item.Name, item.Value);
                    break;
                case KnowledgeItemType.Url:
                    memoryOptions.WebUrls.Add(item.Value);
                    break;
            }
        }

        return null;
    }
}
