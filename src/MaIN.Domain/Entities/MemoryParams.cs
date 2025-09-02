namespace MaIN.Domain.Entities;

public class MemoryParams
{
    public int ContextSize { get; set; } = 8192;
    public int GpuLayerCount { get; set; } = 30;
    public int MaxMatchesCount { get; set; } = 5;
    public float FrequencyPenalty { get; set; } = 1f;
    public float Temperature { get; set; } = 0.6f;
    /// <summary>
    /// Maximum number of tokens to reserve for the answer.
    /// If LLM supports 5000 tokens and AnswerToken is 500 then prompt sent will contain max 4500 tokens
    /// (prompt + question + grounding information from memory)
    /// If your response is invalid make sure you meet those limits.
    /// </summary>
    public int AnswerTokens { get; set; } = 2137;

    public bool MultiModalMode { get; set; } = false;
    
    public string? Grammar { get; set; }
    public bool IncludeQuestionSource { get; set; } = false;
}