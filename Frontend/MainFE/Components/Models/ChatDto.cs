using System.Text.Json.Serialization;
using MaIN.Services.Models;
using MainFE.Components.Pages;

namespace MainFE.Components.Models;

public class ChatDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
    [JsonPropertyName("type")]
    public ChatTypeDto Type { get; set; }
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }
    [JsonPropertyName("visual")]
    public bool Visual { get; set; }
    [JsonIgnore] public bool IsSelected { get; set; }
}