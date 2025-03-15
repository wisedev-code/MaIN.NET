using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class AgentFlowDocument
{
    [BsonId]
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<AgentDocument> Agents { get; init; }
    public required string Description { get; init; }
}