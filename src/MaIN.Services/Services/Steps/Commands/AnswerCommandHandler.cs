using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;

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
        if (command.UseMemory)
        {
            result = await llmService.AskMemory(command.Chat, new ChatMemoryOptions { Memory = command.Chat.Memory});
            return result!.Message;
        }

        result = command.Chat!.Visual
            ? await imageGenService.Send(command.Chat)
            : await llmService.Send(command.Chat, new ChatRequestOptions { InteractiveUpdates = command.Chat.Interactive });

        return result!.Message;
    }
}