using MaIN.Domain.Models;

namespace MaIN.Services.Utils;

public static class NotificationMessageBuilder
{
    public static Dictionary<string, string> CreateActorProgress(
        string agentId, 
        string isProcessing, 
        string? progress, 
        string behaviour,
        string details)
    {
        return new Dictionary<string, string>
        {
            { "AgentId", agentId },
            { "IsProcessing", isProcessing },
            { "State", progress ?? "" },
            { "Behaviour", behaviour },
            { "Details", details }
        };
    }
    
    public static Dictionary<string, string> CreateActorKnowledgeStepProgress(
        string agentId, 
        List<string>? itemNamesAndTasks,
        string model)
    {
        return new Dictionary<string, string>
        {
            { "AgentId", agentId },
            { "Items", string.Join('+', itemNamesAndTasks!) },
            { "Model", model},
        };
    }
    
    public static Dictionary<string, string?> CreateChatCompletion(
        string? chatId, 
        LLMTokenValue content, 
        bool done)
    {
        return new Dictionary<string, string?>
        {
            { "ChatId", chatId },
            { "Done", done.ToString() },
            { "Content", content.Text },
            { "Type", content.Type.ToString() }
        };
    }

    public static Dictionary<string, string> ProcessingTools(string agentId, string behaviour, string details) =>
        CreateActorProgress(agentId, "true", "TOOL", behaviour, details);
    
    public static Dictionary<string, string> ProcessingStarted(string agentId, string behaviour, string details) =>
        CreateActorProgress(agentId, "true", "START", behaviour, details);

    public static Dictionary<string, string> ProcessingComplete(string agentId, string behaviour, string details) =>
        CreateActorProgress(agentId, "false", "DONE", behaviour, details);

    public static Dictionary<string, string> ProcessingFailed(string agentId, string behaviour, string details) =>
        CreateActorProgress(agentId, "false", "FAILED", behaviour, details);
}