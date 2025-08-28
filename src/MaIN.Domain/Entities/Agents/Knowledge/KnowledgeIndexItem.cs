namespace MaIN.Domain.Entities.Agents.Knowledge;

public class KnowledgeIndexItem : IEquatable<KnowledgeIndexItem>
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required KnowledgeItemType Type { get; set; }
    public required string[] Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }

    public bool Equals(KnowledgeIndexItem? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && 
               Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as KnowledgeIndexItem);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name.ToLowerInvariant(), 
            Value.ToLowerInvariant()
        );
    }

    public static bool operator ==(KnowledgeIndexItem? left, KnowledgeIndexItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KnowledgeIndexItem? left, KnowledgeIndexItem? right)
    {
        return !Equals(left, right);
    }
}