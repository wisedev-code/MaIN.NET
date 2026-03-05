using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Steps.Commands;

public class StartCommandHandler : ICommandHandler<StartCommand, Message?>
{
    public Task<Message?> HandleAsync(StartCommand command)
    {
        if (command.Chat.ImageGen)
        {
            return Task.FromResult<Message?>(null);
        }

        var agentId = command.Chat.Properties.GetValueOrDefault(ServiceConstants.Properties.AgentIdProperty, command.Chat.Id);
        if (!ModelRegistry.TryGetById(command.Chat.ModelId, out var model))
        {
            throw new AgentModelNotAvailableException(agentId, command.Chat.ModelId);
        }

        var backend = model!.Backend;
        var message = new Message()
        {
            Content = command.InitialPrompt!,
            Type = backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM,
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
