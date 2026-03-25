namespace MaIN.InferPage.Services;

public class InferPageSettings
{
    public int BackendType { get; set; }
    public bool IsOllamaCloud { get; set; }
    public string? Model { get; set; }
    public bool HasVision { get; set; }
    public bool HasReasoning { get; set; }
    public bool HasImageGen { get; set; }
    public string? ModelPath { get; set; }
    public string? MmProjName { get; set; }
    public string? VertexLocation { get; set; }
}