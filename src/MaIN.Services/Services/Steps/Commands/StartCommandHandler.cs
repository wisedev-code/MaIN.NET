using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class StartCommandHandler : ICommandHandler<StartCommand, Message?>
{
    public Task<Message?> HandleAsync(StartCommand command)
    {
        if (command.Chat.Visual)
        {
            return Task.FromResult<Message?>(null);
        }

        var message = new Message()
        {
            Content = command.InitialPrompt!,
            Type = command.Chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM,
            Role = "System"
        };
        command.Chat.Messages.Add(message);
        
        return Task.FromResult(new Message()
        {
            Content = "STARTED",
            Role = "System",
            Type = message.Type,
            Time = DateTime.UtcNow
        })!;
    }
}