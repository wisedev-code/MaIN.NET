namespace MaIN.Domain.Entities;

public class InferenceParams
{
    public float Temperature { get; init; } = 0.6f;
    public int ContextSize { get; init; } = 1024;
}