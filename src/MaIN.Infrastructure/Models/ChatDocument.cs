using MaIN.Domain.Entities;
using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class ChatDocument
{
    [BsonId]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public List<MessageDocument> Messages { get; set; }
    public ChatTypeDocument Type { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public bool Visual { get; set; }
    public bool Interactive { get; set; }
    public bool Translate { get; set; }
    public InferenceParamsDocument? InferenceParams { get; set; }
}