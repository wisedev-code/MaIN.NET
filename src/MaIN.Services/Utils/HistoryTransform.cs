using System.Text;
using LLama.Abstractions;
using LLama.Common;

namespace MaIN.Services.Utils;

class MaINHistoryTransform : IHistoryTransform
{
    private const string Bos = "<|im_start|>";
    private const string Eos = "<|im_end|>";

    public string HistoryToText(ChatHistory history)
    {
        var sb = new StringBuilder(1024);

        foreach (var message in history.Messages)
        {
            sb.Append(Bos);
            sb.Append(GetRoleName(message.AuthorRole));
            sb.Append('\n');
            sb.Append(message.Content);
            sb.Append(Eos);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string GetRoleName(AuthorRole authorRole)
    {
        return authorRole switch
        {
            AuthorRole.User => "user",
            AuthorRole.Assistant => "assistant",
            AuthorRole.System => "system",
            _ => throw new Exception($"Unsupported role: {authorRole}"),
        };
    }

    public ChatHistory TextToHistory(AuthorRole role, string text)
    {
        return new ChatHistory([new ChatHistory.Message(role, text)]);
    }

    public IHistoryTransform Clone()
    {
        return new MaINHistoryTransform();
    }
}