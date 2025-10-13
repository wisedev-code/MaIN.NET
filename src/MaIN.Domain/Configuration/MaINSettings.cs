
using MaIN.Services.Configuration;

namespace MaIN.Domain.Configuration;

public class MaINSettings
{
    public BackendType BackendType { get; set; } = BackendType.Self;
    public string? ModelsPath { get; set; }
    public string? ImageGenUrl { get; set; }
    public string? OpenAiKey { get; set; }
    public string? GeminiKey { get; set; }
    public string? DeepSeekKey { get; set; }
    public string? AnthropicKey { get; set; }
    public string? GroqCloudKey { get; set; }
    public ImageGenSettings? ImageGenSettings { get; set; }
    public MongoDbSettings? MongoDbSettings { get; set; }
    public FileSystemSettings? FileSystemSettings { get; set; }
    public SqliteSettings? SqliteSettings { get; set; }
    public SqlSettings? SqlSettings { get; set; }
    public string? VoicesPath { get; set; }
}

public enum BackendType
{
    Self = 0,
    OpenAi = 1,
    Gemini = 2,
    DeepSeek = 3,
    GroqCloud = 4,
    Anthropic = 5,
    ONNX = 6,
}

public class ImageGenSettings
{
    public string ExecutionProviderTarget { get; set; } = "Cpu"; // Options: "Cpu", "DirectML"
    public int NumInferenceSteps { get; set; } = 8;
    public double GuidanceScale { get; set; } = 7.5;
    public string ModelPath { get; set; } = string.Empty;
}