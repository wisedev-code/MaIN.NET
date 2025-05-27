using MaIN.Domain.Entities;

namespace MaIN.Services.Services.Steps;

public class StepHandlerExtensions
{
    public static Chat EnsureUserMessageReadiness(Chat chat)
    {
        if (chat.Messages.Count == 0)
            return chat;
        
        var lastMessage = chat.Messages.LastOrDefault();
        if (lastMessage == null || lastMessage.Role== "User")
            return chat;
    
        var lastUserMessage = chat.Messages.LastOrDefault(m => m.Role == "User");
        if (lastUserMessage == null)
            return chat; // No user messages
    
        var newMessages = chat.Messages
            .Where(m => m != lastUserMessage)
            .ToList();
        newMessages.Add(lastUserMessage);

        chat.Messages = newMessages;
        return chat;
    }
}