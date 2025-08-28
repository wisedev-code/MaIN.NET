
using System.Text.Json;

namespace MaIN.Domain.Entities.Agents.Knowledge;

public class KnowledgeIndex
{
    public List<KnowledgeIndexItem> Items { get; set; } = new();
    
    public int Count => Items.Count;
    
    public bool IsEmpty => Items.Count == 0;

    public IReadOnlyList<string> GetAllTags(bool ignoreCase = true)
    {
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        
        return Items
            .SelectMany(item => item.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(comparer)
            .OrderBy(tag => tag, comparer)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyDictionary<string, int> GetTagUsageStatistics(bool ignoreCase = true)
    {
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        
        return Items
            .SelectMany(item => item.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag, comparer)
            .ToDictionary(
                group => group.Key, 
                group => group.Count(),
                comparer
            )
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetMostUsedTags(int topCount = 10, bool ignoreCase = true)
    {
        var tagStats = GetTagUsageStatistics(ignoreCase);
        
        return tagStats
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Take(topCount)
            .Select(kvp => kvp.Key)
            .ToList()
            .AsReadOnly();
    }

    public string AsString() =>
        JsonSerializer.Serialize(this);

}