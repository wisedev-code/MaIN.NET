using MaIN.Services.Services.Abstract;
using Microsoft.AspNetCore.SignalR;

namespace MaIN.Services.Services;

public class SignalRNotificationService(IHubContext<NotificationHub> hub) : INotificationService
{
    public async Task DispatchNotification(object message, string messageType)
    {
        var msg = message as Dictionary<string,string>;
        object? payload = null;

        switch (messageType)
        {
            case "ReceiveAgentUpdate":
                payload = new
                {
                    agentId = msg!["AgentId"],
                    isProcessing = bool.Parse(msg["IsProcessing"]),
                    behaviour = msg["Behaviour"],
                    progress = msg["Progress"]
                };
                break;
            case "ReceiveMessageUpdate":
                payload = new
                {
                    content = msg!["Content"],
                    done = bool.Parse(msg["Done"]),
                    chatId = msg["ChatId"]
                };
                break;
        }

        // Send the payload to all clients
        await hub.Clients.All.SendAsync(messageType, payload);
    }
}