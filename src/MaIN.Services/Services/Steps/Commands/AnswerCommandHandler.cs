using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class AnswerCommandHandler : ICommandHandler<AnswerCommand, Message?>
{
    private readonly ILLMService _llmService;
    private readonly IImageGenService _imageGenService;

    public AnswerCommandHandler(
        ILLMService llmService,
        IImageGenService imageGenService)
    {
        _llmService = llmService;
        _imageGenService = imageGenService;
    }

    public async Task<Message?> HandleAsync(AnswerCommand command)
    {
        ChatResult? result;
        
        if (command.UseMemory)
        {
            result = await _llmService.AskMemory(command.Chat!, memory: command.Chat?.Memory);
        }
        else
        {
            result = command.Chat!.Visual
                ? await _imageGenService.Send(command.Chat)
                : await _llmService.Send(command.Chat, interactiveUpdates: command.Chat.Interactive);
        }

        return result!.Message.ToDomain();
    }
}