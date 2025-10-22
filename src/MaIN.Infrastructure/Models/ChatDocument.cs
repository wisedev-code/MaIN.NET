using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Tools;
using MongoDB.Bson.Serialization.Attributes;

namespace MaIN.Infrastructure.Models;

public class ChatDocument
{
    [BsonId]
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Model { get; init; }
    public required List<MessageDocument> Messages { get; init; }
    public ChatTypeDocument Type { get; init; }
    public required Dictionary<string, string> Properties { get; init; } = [];
    public BackendType? Backend { get; set; }
    public bool Visual { get; init; }
    public bool Interactive { get; init; }
    public bool Translate { get; init; }
    public InferenceParamsDocument? InferenceParams { get; init; }
    public MemoryParamsDocument? MemoryParams { get; init; }
    public object? ConvState { get; init; }
    public ToolsConfiguration? ToolsConfiguration { get; set; }
}