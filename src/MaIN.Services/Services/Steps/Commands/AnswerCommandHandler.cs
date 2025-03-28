using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler(
    ILLMService llmService,
    IImageGenService imageGenService)
    : ICommandHandler<AnswerCommand, Message?>
{
    public async Task<Message?> HandleAsync(AnswerCommand command)
    {
        ChatResult? result;
        
        if (command.UseMemory)
        {
            result = await llmService.AskMemory(command.Chat!, memory: command.Chat?.Memory);
            return result!.Message;
        }

        result = command.Chat!.Visual
            ? await imageGenService.Send(command.Chat)
            : await llmService.Send(command.Chat, interactiveUpdates: command.Chat.Interactive);

        return result!.Message;
    }
}