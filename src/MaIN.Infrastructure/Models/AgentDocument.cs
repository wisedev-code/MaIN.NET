using MaIN.Domain.Configuration;
using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class AgentDocument
{
    [BsonId]
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Model { get; init; }
    public string? Description { get; init; }
    public bool Started { get; init; }
    public AgentContextDocument? Context { get; init; }
    public required string ChatId { get; set; }
    public int Order { get; init; }
    public BackendType? Backend { get; init; }
    public Dictionary<string, string> Behaviours { get; init; } = [];
    public string CurrentBehaviour { get; set; } = null!;
    public bool Flow { get; init; }
}