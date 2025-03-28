namespace MaIN.Domain.Models;

public class ThinkingState
{
    public bool IsInThinkingMode { get; set; }
    public Dictionary<string, string> Props { get; set; } = new();
}