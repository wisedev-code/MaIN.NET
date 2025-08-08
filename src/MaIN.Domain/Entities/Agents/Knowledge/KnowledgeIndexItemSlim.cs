namespace MaIN.Domain.Entities.Agents.Knowledge;

public class KnowledgeIndexItemSlim
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required KnowledgeItemType Type { get; set; }
}