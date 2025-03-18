using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class StartCommandHandler : ICommandHandler<StartCommand, Message?>
{
    public Task<Message?> HandleAsync(StartCommand command)
    {
        if (command.Chat?.Visual == true)
        {
            return Task.FromResult<Message?>(null);
        }

        var message = new Message()
        {
            Content = command.InitialPrompt!,
            Role = "System"
        };
        command.Chat?.Messages?.Add(message);
        
        return Task.FromResult(new Message()
        {
            Content = "STARTED",
            Role = "System",
            Time = DateTime.UtcNow
        })!;
    }
}