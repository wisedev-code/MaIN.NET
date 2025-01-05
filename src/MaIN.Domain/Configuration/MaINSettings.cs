
using MaIN.Services.Configuration;

namespace MaIN.Domain.Configuration;

public class MaINSettings
{
    public string? ModelsPath { get; set; }
    public string? ImageGenUrl { get; set; }
    public string? OllamaUrl { get; set; }
    public MongoDbSettings? MongoDbSettings { get; set; }
    public FileSystemSettings? FileSystemSettings { get; set; }
    public SqliteSettings? SqliteSettings { get; set; }
    public SqlSettings? SqlSettings { get; set; }
}