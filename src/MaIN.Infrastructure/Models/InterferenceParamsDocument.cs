namespace MaIN.Infrastructure.Models;

public class InferenceParamsDocument
{
    public float Temperature { get; set; } = 0.8f;
    public int ContextSize { get; set; } = 1024;
}