namespace MaIN.Domain.Entities;

public class InferenceParams
{
    public float Temperature { get; init; } = 0.8f;
    public int ContextSize { get; init; } = 1024;
}