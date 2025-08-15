using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Services.Constants;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Models.Utils;

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler(
    ILLMServiceFactory llmServiceFactory,
    IMcpService mcpService,
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
                    return await ProcessKnowledgeQuery(command.Knowledge, command.Chat);
                }

                break;
            case KnowledgeUsage.AlwaysUseKnowledge:
                return await ProcessKnowledgeQuery(command.Knowledge, command.Chat);
        }

        result = command.Chat!.Visual
            ? await imageGenService.Send(command.Chat)
            : await llmService.Send(command.Chat,
                new ChatRequestOptions { InteractiveUpdates = command.Chat.Interactive });

        return result!.Message;
    }

    private async Task<bool> ShouldUseKnowledge(Knowledge? knowledge, Chat chat)
    {
        var originalContent = chat.Messages.Last().Content;
        
        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, JsonOptions);

        chat.MemoryParams.Grammar = ServiceConstants.Grammars.DecisionGrammar;
        chat.Messages.Last().Content =
            $"Based on this conversation and following prompt, you should decide if you want to use knowledge or not. Content of available knowledge is stored in your memory, Prompt: {originalContent}";

        var service = llmServiceFactory.CreateService(chat.Backend ?? settings.BackendType);
        var memoryOptions = new ChatMemoryOptions
        {
            TextData = new Dictionary<string, string> { { "knowledge_index.json", index } }
        };

        var result = await service.AskMemory(chat, memoryOptions);
        var decision = JsonSerializer.Deserialize<DecisionResult>(result!.Message.Content, JsonOptions);

        chat.Messages.Last().Content = originalContent;
        return decision?.Decision ?? false;
    }

    private async Task<Message?> ProcessKnowledgeQuery(Knowledge? knowledge, Chat chat)
    {
        var originalContent = chat.Messages.Last().Content;
        var indexAsKnowledge = knowledge?.Index.Items.ToDictionary(x => x.Name, x => x.Tags);
        var index = JsonSerializer.Serialize(indexAsKnowledge, JsonOptions);

        chat.MemoryParams.Grammar = ServiceConstants.Grammars.KnowledgeGrammar;
        chat.Messages.Last().Content =
            $"""
             Find tags that fits user query based on available knowledge. 
             Content of available knowledge is stored in your memory. Try to be strict to include only relevant matches, 
             You should not provide more than 4 matches. Prompt: {originalContent}
             """;

        var llmService = llmServiceFactory.CreateService(chat.Backend ?? settings.BackendType);
        var memoryOptions = new ChatMemoryOptions
        {
            TextData = new Dictionary<string, string> { { "knowledge_index.json", index! } }
        };

        var searchResult = await llmService.AskMemory(chat, memoryOptions);
        var matchedTags = JsonSerializer.Deserialize<List<string>>(searchResult!.Message.Content, JsonOptions);

        chat.Messages.Last().Content = originalContent;
        chat.MemoryParams.IncludeQuestionSource = true;
        chat.MemoryParams.Grammar = null;
        memoryOptions.TextData.Clear();
        
        var knowledgeItems = knowledge!.Index.Items
            .Where(x => x.Tags
                .Intersect(matchedTags!)
                .Any())
            .ToList();

        //NOTE: perhaps good idea for future to combine knowledge form MCP and from KM 
        var mcpConfig = BuildMemoryOptionsFromKnowledgeItems(knowledgeItems, memoryOptions);
        if (mcpConfig != null)
        {
            var result = await mcpService.Prompt(mcpConfig, chat.Messages.Last().Content);
            return result.Message;
        }
        
        var knowledgeResult = await llmService.AskMemory(chat, memoryOptions);
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