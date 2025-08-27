using System.Text.Json.Serialization;

namespace MaIN.Domain.Entities.Agents.Knowledge;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KnowledgeItemType
{
    File = 0,
    Url = 1,
    Text = 2,
    Mcp = 3
}