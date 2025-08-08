using System.Text.Json.Serialization;

namespace MaIN.Domain.Entities.Agents.Knowledge;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KnowledgeItemType
{
    File,
    Url,
    Text,
}