using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler(
    ILLMServiceFactory llmServiceFactory,
    IMcpService mcpService,
    INotificationService notificationService,
    IImageGenServiceFactory imageGenServiceFactory,
    MaINSettings settings)
    : ICommandHandler<AnswerCommand, Message?>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Message?> HandleAsync(AnswerCommand command)
    {
        ChatResult? result;
        var llmService = llmServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType);
        var imageGenService = imageGenServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType);

        switch (command.KnowledgeUsage)
        {
            case KnowledgeUsage.UseMemory:
                result = await llmService.AskMemory(command.Chat,
                    new ChatMemoryOptions { Memory = command.Chat.Memory });
                return result!.Message;
            case KnowledgeUsage.UseKnowledge:
                var isKnowledgeNeeded = await ShouldUseKnowledge(command.Knowledge, command.Chat);
                if (isKnowledgeNeeded)
                {
                    return await ProcessKnowledgeQuery(command.Knowledge, command.Chat, command.AgentId);
                }

                break;
            case KnowledgeUsage.AlwaysUseKnowledge:
                return await ProcessKnowledgeQuery(command.Knowledge, command.Chat, command.AgentId);
        }

        result = command.Chat.Visual
            ? await imageGenService!.Send(command.Chat)
            : await llmService.Send(command.Chat,
                new ChatRequestOptions { InteractiveUpdates = command.Chat.Interactive });

        return result!.Message;
    }

    private async Task<bool> ShouldUseKnowledge(Knowledge? knowledge, Chat chat)
    {
        var originalContent = chat.Messages.Last().Content;

        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, JsonOptions);

        chat.InterferenceParams.Grammar = new Grammar(ServiceConstants.Grammars.DecisionGrammar, GrammarFormat.GBNF);
        chat.Messages.Last().Content =
            $"""
             KNOWLEDGE:
             {index}

                   Based on the following prompt, decide if you should use external knowledge.
                   Use external knowledge unless you're certain the question requires only basic facts (like "What is 2+2?" or "Capital of France?").
                   When in doubt, use it - external knowledge often provides more current, specific, or contextual information.
                   Content of available knowledge has source tags. Prompt: {originalContent}
             """;

        var service = llmServiceFactory.CreateService(chat.Backend ?? settings.BackendType);

        var result = await service.Send(chat, new ChatRequestOptions()
        {
            SaveConv = false
        });
        var decision = JsonSerializer.Deserialize<JsonElement>(result!.Message.Content, JsonOptions);
        var decisionValue = decision.GetProperty("decision").GetRawText();
        chat.InterferenceParams.Grammar = null;
        var shouldUseKnowledge = bool.Parse(decisionValue.Trim('"'));
        chat.Messages.Last().Content = originalContent;
        return shouldUseKnowledge!;
    }

    private async Task<Message?> ProcessKnowledgeQuery(Knowledge? knowledge, Chat chat, string agentId)
    {
        var originalContent = chat.Messages.Last().Content;
        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, JsonOptions);

        chat.InterferenceParams.Grammar = new Grammar(ServiceConstants.Grammars.KnowledgeGrammar, GrammarFormat.GBNF);
        chat.Messages.Last().Content =
            $"""
             KNOWLEDGE:
             {index}

             Find tags that fits user query based on available knowledge (provided to you above as pair of item names with tags). 
             Always return at least 1 tag in array, and no more than 4. Prompt: {originalContent}
             """;

        var llmService = llmServiceFactory.CreateService(chat.Backend ?? settings.BackendType);

        var searchResult = await llmService.Send(chat, new ChatRequestOptions()
        {
            SaveConv = false
        });
        var matchedTags = JsonSerializer.Deserialize<List<string>>(searchResult!.Message.Content, JsonOptions);
        var knowledgeItems = knowledge!.Index.Items
            .Where(x => x.Tags.Intersect(matchedTags!).Any() ||
                        matchedTags!.Contains(x.Name))
            .ToList();
        
        //NOTE: perhaps good idea for future to combine knowledge form MCP and from KM 
        var memoryOptions = new ChatMemoryOptions();
        var mcpConfig = BuildMemoryOptionsFromKnowledgeItems(knowledgeItems, memoryOptions);

        chat.Messages.Last().Content = $"{originalContent} - Use information given you as memory.";
        chat.MemoryParams.IncludeQuestionSource = true;
        chat.MemoryParams.Grammar = null;
        
        await notificationService.DispatchNotification(NotificationMessageBuilder.CreateActorKnowledgeStepProgress(
            agentId,
            knowledgeItems.Select(x => $" {x.Name}|{x.Type} ").ToList(),
            mcpConfig?.Model ?? chat.Model), "ReceiveAgentUpdate");
        
        if (mcpConfig != null)
        {
            var result = await mcpService.Prompt(mcpConfig, chat.Messages);
            return result.Message;
        }

        var knowledgeResult = await llmService.AskMemory(chat, memoryOptions);
        chat.Messages.Last().Content = originalContent;
        return knowledgeResult?.Message;
    }

    private static Mcp? BuildMemoryOptionsFromKnowledgeItems(List<KnowledgeIndexItem>? knowledgeItems,
        ChatMemoryOptions memoryOptions)
    {
        //First or default because we cannot combine response from multiple servers in one go at the moment
        var mcp = knowledgeItems?.FirstOrDefault(x => x.Type == KnowledgeItemType.Mcp);
        if (mcp != null)
        {
            return JsonSerializer.Deserialize<Mcp>(mcp.Value, JsonOptions);
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