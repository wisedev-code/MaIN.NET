namespace MaIN.Infrastructure.Models;

public class InferenceParamsDocument
{
    public float Temperature { get; set; }
    public int ContextSize { get; set; }
    public int GpuLayerCount { get; init; } 
    public uint SeqMax { get; init; } 
    public uint BatchSize { get; init; }
    public uint UBatchSize { get; init; } 
    public bool Embeddings { get; init; } 
    public int TypeK { get; init; } 
    public int TypeV { get; init; } 
    public int TokensKeep { get; set; }
    public int MaxTokens { get; set; }
    public int TopK { get; init; }
    public float TopP { get; init; }
}