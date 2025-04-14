namespace MaIN.Infrastructure.Models;

public class MemoryParamsDocument
{
    public int ContextSize { get; set; }
    public int GpuLayerCount { get; set; }
    public int MaxMatchesCount { get; set; } 
    public float FrequencyPenalty { get; set; }
    public float Temperature { get; set; } 
    public int AnswerTokens { get; set; }
}