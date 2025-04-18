using MaIN.Domain.Entities;

namespace MaIN.Domain.Extensions;

public static class ChatExtensions
{
    public static bool UsePreProcessorDocuments(this Chat chat)
    {
        return chat.Properties.ContainsKey("PreProcessFiles");
    }
}