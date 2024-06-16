using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class ChatDocument
{
    [BsonId]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public List<MessageDocument> Messages { get; set; }
    public bool Stream { get; set; } = false;
}