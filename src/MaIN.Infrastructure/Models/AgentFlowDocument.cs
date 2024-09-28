using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class AgentFlowDocument
{
    [BsonId]
    public string Id { get; set; }
    public string Name { get; set; }
    public List<AgentDocument> Agents { get; set; }
}