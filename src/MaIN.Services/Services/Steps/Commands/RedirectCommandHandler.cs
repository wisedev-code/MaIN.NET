using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class RedirectCommandHandler(IAgentService agentService) : ICommandHandler<RedirectCommand, Message?>
{
    public async Task<Message?> HandleAsync(RedirectCommand command)
    {
        var chat = await agentService.GetChatByAgent(command.RelatedAgentId);
        chat.Messages.Add(new Message()
        {
            Role = "User",
            Content = command.Message.Content,
            Properties = new Dictionary<string, string>()
            {
                { "agent_internal", "true" }
            },
            Type = chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM
        });

        if (!string.IsNullOrEmpty(command.Filter))
        {
            chat.Properties.TryAdd("data_filter", command.Filter!);
        }

        var result = await agentService.Process(chat, command.RelatedAgentId);
        return new Message()
        {
            Content = result.Messages.Last().Content,
            Image = result.Messages.Last().Image,
            Role = "System",
            Type = chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM,
            Properties = new Dictionary<string, string>()
            {
                { "agent_internal", "true" }
            }
        };
    }
}