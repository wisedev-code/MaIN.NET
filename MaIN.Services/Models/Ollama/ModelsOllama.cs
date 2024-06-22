using System.Text.Json.Serialization;

namespace MaIN.Services.Models.Ollama;

public class ModelsOllamaResponse
{
    [JsonPropertyName("models")] 
    public List<ModelsOllama> Models { get; set; }
}

public class ModelsOllama
{
    [JsonPropertyName("name")] 
    public string Name { get; set; }
}