namespace MaIN.Domain.Models;

public class LLMTokenValue
{
    public required string Text { get; set; }
    public TokenType Type { get; set; }
}