namespace MaIN.Services.Utils;

public static class NotificationMessageBuilder
{
    public static Dictionary<string, string> CreateActorProgress(
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
    
    public static Dictionary<string, string?> CreateChatCompletion(
        string? chatId, 
        string? content, 
        bool done)
    {
        return new Dictionary<string, string?>
        {
            { "ChatId", chatId },
            { "Done", done.ToString() },
            { "Content", content }
        };
    }

    public static Dictionary<string, string> ProcessingStarted(string agentId, string behaviour) =>
        CreateActorProgress(agentId, "true", null, behaviour);

    public static Dictionary<string, string> ProcessingComplete(string agentId, string behaviour) =>
        CreateActorProgress(agentId, "false", "DONE", behaviour);

    public static Dictionary<string, string> ProcessingFailed(string agentId, string behaviour) =>
        CreateActorProgress(agentId, "false", "FAILED", behaviour);
}