namespace MaIN.Domain.Entities;

public class InferenceParams
{
    public float Temperature { get; init; } = 0.8f;
    public int ContextSize { get; init; } = 1024;
    public int GpuLayerCount { get; init; } = 30;
    public uint SeqMax { get; init; } = 1;
    public uint BatchSize { get; init; } = 512;
    public uint UBatchSize { get; init; } = 512;
    public bool Embeddings { get; init; } = false;
    public int TypeK { get; init; } = 0;
    public int TypeV { get; init; } = 0;
    
    public int TokensKeep { get; set; }
    public int MaxTokens { get; set; }
    
    public int TopK { get; init; } = 40;
    public float TopP { get; init; } = 0.9f;
}