using Microsoft.AspNetCore.SignalR;

namespace MaIN;

public class NotificationHub : Hub
{
    public async Task UpdateAgentProcessing(string agentId, bool isProcessing)
    {
        // Prepare the JSON payload to send to all clients
        var payload = new
        {
            agentId = agentId,
            isProcessing = isProcessing
        };

        // Send the payload to all clients
        await Clients.All.SendAsync("ReceiveAgentUpdate", payload);
    }
}