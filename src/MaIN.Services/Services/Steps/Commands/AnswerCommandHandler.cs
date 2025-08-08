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

#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler(
    ILLMServiceFactory llmServiceFactory,
    IImageGenServiceFactory imageGenServiceFactory,
    MaINSettings settings)
    : ICommandHandler<AnswerCommand, Message?>
{
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
                var isKnowledgeNeeded = await DecideIfKnowledgeNeeded(command.Knowledge, command.Chat);
                if (isKnowledgeNeeded)
                {
                    return await AskKnowledge(command.Knowledge, command.Chat);
                }

                break;
            case KnowledgeUsage.AlwaysUseKnowledge:
                return await AskKnowledge(command.Knowledge, command.Chat);
        }


        result = command.Chat!.Visual
            ? await imageGenService.Send(command.Chat)
            : await llmService.Send(command.Chat,
                new ChatRequestOptions { InteractiveUpdates = command.Chat.Interactive });

        return result!.Message;
    }

    private async Task<bool> DecideIfKnowledgeNeeded(Knowledge? commandKnowledge, Chat commandChat)
    {
        var lastMessageContent = commandChat.Messages.Last().Content;
        var index = commandKnowledge?.Index.AsString();
        commandChat.MemoryParams.Grammar = ServiceConstants.Grammars.DecisionGrammar;
        commandChat.Messages.Last().Content =
            $"Based on this conversation and following prompt, you should decide if you want to use knowledge or not. Content of available knowledge is stored in your memory, Prompt: {lastMessageContent}";
        var service = //perhaps its already created
            llmServiceFactory.CreateService(commandChat.Backend ?? settings.BackendType);
        var memoryOptions = new ChatMemoryOptions
        {
            TextData = new Dictionary<string, string> { { "knowledge_index.json", index! } }
        };
        var result = await service.AskMemory(commandChat, memoryOptions);
        var res = JsonSerializer.Deserialize<DecisionResult>(result!.Message.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        commandChat.Messages.Last().Content = lastMessageContent;
        return res?.Decision ?? false;
    }

    private async Task<Message?> AskKnowledge(Knowledge? knowledge, Chat commandChat)
    {
        var lastMessage = commandChat.Messages.Last();
        var index = knowledge?.Index.AsString();
        commandChat.MemoryParams.Grammar = ServiceConstants.Grammars.KnowledgeGrammar;
        commandChat.Messages.Last().Content =
            $"Find matches based on names and tags in available knowledge. Content of available knowledge is stored in your memory, Prompt: {lastMessage.Content}";
        var llmService = llmServiceFactory.CreateService(commandChat.Backend ?? settings.BackendType);
        var memoryOptions = new ChatMemoryOptions
        {
            TextData = new Dictionary<string, string> { { "knowledge_index.json", index! } }
        };
        var result = await llmService.AskMemory(commandChat,
            memoryOptions);
        var itemsSet = JsonSerializer.Deserialize<KnowledgeIndexCheckResult>(result!.Message.Content);
        commandChat.Messages.Last().Content = lastMessage.Content;
        commandChat.MemoryParams.IncludeQuestionSource = true;
        memoryOptions.TextData.Clear();
        foreach (var item in itemsSet!.FetchedItems)
        {
            switch (item.Type)
            {
                case KnowledgeItemType.File:
                    memoryOptions.FilesData?.Add(item.Name, item.Value);
                    break;
                case KnowledgeItemType.Text:
                    memoryOptions.TextData?.Add(item.Name, item.Value);
                    break;
                case KnowledgeItemType.Url:
                    memoryOptions.WebUrls?.Add(item.Value);
                    break;
            }
        }

        var knowledgeResult = await llmService.AskMemory(commandChat, memoryOptions);
        return knowledgeResult?.Message;
    }
}