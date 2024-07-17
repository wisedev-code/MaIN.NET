using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class AgentDocument
{
    [BsonId]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public string? Description { get; set; }
    public bool Started { get; set; }
    public AgentContextDocument Context { get; set; }
    public string? ChatId { get; set; }
}