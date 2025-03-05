namespace MaIN.Domain.Entities;

public class InferenceParams
{
    public float Temperature { get; set; } = 0.8f;
    public int ContextSize { get; set; } = 1024;
}