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
        //TODO_PIH Usage of knowledge means that we use KM integration to fetch correct files/sources that main contains
        // data we need by available tags, and only then we use KM again to ask for answer in those files
        ChatResult? result;
        var llmService = llmServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType);
        var imageGenService = imageGenServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType);
        switch (command.KnowledgeUsage)
        {
            case KnowledgeUsage.UseMemory:
                result = await llmService.AskMemory(command.Chat, new ChatMemoryOptions { Memory = command.Chat.Memory});
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
                break;
        }


        result = command.Chat!.Visual
            ? await imageGenService.Send(command.Chat)
            : await llmService.Send(command.Chat, new ChatRequestOptions { InteractiveUpdates = command.Chat.Interactive });

        return result!.Message;
    }

    private async Task<bool> DecideIfKnowledgeNeeded(Knowledge? commandKnowledge, Chat commandChat)
    {
        var lastMessage = commandChat.Messages.Last();
        var index = commandKnowledge?.Index.AsString();
        commandChat.Messages.Last().Content = $"Based on this conversation and following prompt, you should decide if you want to use knowledge or not. Content of available knowledge is stored in your memory, Prompt: {lastMessage.Content}";
        var result = await 
            llmServiceFactory.CreateService(commandChat.Backend ?? settings.BackendType)
                .AskMemory(commandChat, new ChatMemoryOptions { TextData = {{"knowledge_index.json", index!}}});
        return result!.Message.Content.Contains("yes");
    }

    private async Task<Message?> AskKnowledge(Knowledge? knowledge, Chat commandChat)
    {
        throw new NotImplementedException();
    }
}