
using MaIN.Services.Configuration;

namespace MaIN.Domain.Configuration;

public class MaINSettings
{
    public string ModelsPath { get; set; }
    public string ImageGenUrl { get; set; }
    public string OllamaUrl { get; set; }
    public MongoDbSettings MongoDbSettings { get; set; }
}