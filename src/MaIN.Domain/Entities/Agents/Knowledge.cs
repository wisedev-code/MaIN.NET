namespace MaIN.Domain.Entities.Agents;

internal class Knowledge
{
    
}

file class KnowledgeIndex
{
    public string[] Files { get; set; }
    public List<K
}

file class KnowledgeIndexItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public KnowledgeItemType Type { get; set; }
    public string[] Tags { get; set; }
}

public enum KnowledgeItemType
{
    File,
    Url
}
