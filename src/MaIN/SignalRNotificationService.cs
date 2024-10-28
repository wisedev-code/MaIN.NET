using System.Drawing;
using System.Text.Json;
using MaIN.Services.Services.Abstract;
using Microsoft.AspNetCore.SignalR;

namespace MaIN.Services.Services;

public class SignalRNotificationService(IHubContext<NotificationHub> hub) : INotificationService
{
    public async Task DispatchNotification(object message)
    {
        var msg = message as Dictionary<string,string>;
        var payload = new
        {
            agentId = msg!["AgentId"],
            isProcessing = bool.Parse(msg!["IsProcessing"]),
            behaviour = msg!["Behaviour"],
            progress = msg!["Progress"]
        };

        // Send the payload to all clients
        await hub.Clients.All.SendAsync("ReceiveAgentUpdate", payload);
    }
}