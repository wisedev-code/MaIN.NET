using MaIN.Domain.Models;

namespace MaIN.Infrastructure.Models;

public class LLMTokenValueDocument
{
    public required string Text { get; set; }
    public TokenType Type { get; set; } //TODO add document representation of this domain enum
}