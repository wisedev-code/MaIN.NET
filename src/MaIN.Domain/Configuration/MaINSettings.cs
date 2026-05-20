using MaIN.Domain.Configuration.Vertex;

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
    public string? OllamaKey { get; set; }
    public string? XaiKey { get; set; }
    public MongoDbSettings? MongoDbSettings { get; set; }
    public FileSystemSettings? FileSystemSettings { get; set; }
    public SqliteSettings? SqliteSettings { get; set; }
    public SqlSettings? SqlSettings { get; set; }
    public string? VoicesPath { get; set; }
    public GoogleServiceAccountConfig? GoogleServiceAccountAuth { get; set; }
    public string? SkillsDirectory { get; set; }
    public SkillUploadSettings SkillUpload { get; set; } = new();
}

public class SkillUploadSettings
{
    /// <summary>Path to the on-disk cache of provider skill ids. Avoids re-uploading bundles on each run.</summary>
    public string CacheFilePath { get; set; } = Path.Combine(".main", "skills-cache.json");

    /// <summary>
    /// When true, before uploading to a backend for the first time in this process, the coordinator
    /// fetches the provider's existing skill list (GET /v1/skills) and matches by name to recover
    /// from a wiped cache file without creating duplicate uploads. The endpoint is not officially
    /// documented for OpenAI / Anthropic; if it 404s the coordinator logs a warning and proceeds
    /// without reconciliation. Default on — degrades gracefully when the endpoint is missing.
    /// </summary>
    public bool ReconcileWithProvider { get; set; } = true;
}

public enum BackendType
{
    Self = 0,
    OpenAi = 1,
    Gemini = 2,
    DeepSeek = 3,
    GroqCloud = 4,
    Anthropic = 5,
    Xai = 6,
    Ollama = 7,
    Vertex = 8,
}