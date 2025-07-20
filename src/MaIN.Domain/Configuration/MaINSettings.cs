
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
    public string? GroqCloudKey { get; set; }
    public MongoDbSettings? MongoDbSettings { get; set; }
    public FileSystemSettings? FileSystemSettings { get; set; }
    public SqliteSettings? SqliteSettings { get; set; }
    public SqlSettings? SqlSettings { get; set; }
}

public enum BackendType
{
    Self = 0,
    OpenAi = 1,
    Gemini = 2,
    DeepSeek = 3,
    GroqCloud = 4,
}