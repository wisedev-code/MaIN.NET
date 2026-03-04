using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Steps.Commands;

public class RedirectCommandHandler(IAgentService agentService) : ICommandHandler<RedirectCommand, Message?>
{
    public async Task<Message?> HandleAsync(RedirectCommand command)
    {
        var chat = await agentService.GetChatByAgent(command.RelatedAgentId);
        var backend = ModelRegistry.GetById(chat.ModelId).Backend;
        chat.Messages.Add(new Message()
        {
            Role = "User",
            Content = command.Message.Content,
            Properties = new Dictionary<string, string>()
            {
                { "agent_internal", "true" },
                { Message.UnprocessedMessageProperty, string.Empty}
            },
            Type = backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM
        });

        if (!string.IsNullOrEmpty(command.Filter))
        {
            chat.Properties.TryAdd("data_filter", command.Filter!);
        }

        //TODO perhaps we want to be able to transfer knowledge?
        var result = await agentService.Process(chat, command.RelatedAgentId, null);
        return new Message()
        {
            Content = result.Messages.Last().Content,
            Image = result.Messages.Last().Image,
            Role = "System",
            Type = backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM,
            Properties = new Dictionary<string, string>()
            {
                { "agent_internal", "true" },
                { Message.UnprocessedMessageProperty, string.Empty }
            }
        };
    }
}
