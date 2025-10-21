namespace MaIN.Domain.Models;

public enum TokenType
{
    FullAnswer = 0,
    Message = 1,
    Reason = 2,
    Special = 3,
    ToolCall = 4,
}