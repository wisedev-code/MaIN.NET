using System.Text.Json.Serialization;
using MainFE.Components.Pages;

namespace MainFE.Components.Models;

public class ChatDto
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("model")] public string Model { get; set; }
    [JsonPropertyName("messages")] public List<Message> Messages { get; set; }
    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
    [JsonIgnore] public bool IsSelected { get; set; }
}