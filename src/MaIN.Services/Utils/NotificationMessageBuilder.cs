namespace MaIN.Services.Utils;

public static class NotificationMessageBuilder
{
    public static Dictionary<string, string> Create(
        string agentId, 
        string isProcessing, 
        string? progress, 
        string behaviour)
    {
        return new Dictionary<string, string>
        {
            { "AgentId", agentId },
            { "IsProcessing", isProcessing },
            { "Progress", progress ?? "" },
            { "Behaviour", behaviour }
        };
    }

    public static Dictionary<string, string> ProcessingStarted(string agentId, string behaviour) =>
        Create(agentId, "true", null, behaviour);

    public static Dictionary<string, string> ProcessingComplete(string agentId, string behaviour) =>
        Create(agentId, "false", "DONE", behaviour);

    public static Dictionary<string, string> ProcessingFailed(string agentId, string behaviour) =>
        Create(agentId, "false", "FAILED", behaviour);
}