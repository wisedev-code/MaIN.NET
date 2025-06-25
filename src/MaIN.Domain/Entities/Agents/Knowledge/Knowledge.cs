using System.Text.Json;
using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities.Agents.Knowledge;

public class Knowledge(Agent agent, KnowledgeIndex? index = null, bool persistenceEnabled = true)
{
    private MaINSettings? _settings;
    
    public KnowledgeIndex Index { get; set; } = index ?? new KnowledgeIndex();
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
    public Agent Agent { get; } = agent ?? throw new ArgumentNullException(nameof(agent));
    public bool PersistenceEnabled { get; private set; } = persistenceEnabled;

    private string GetKnowledgePath() => Path.Combine("Knowledge", $"{Agent.Name}+{Agent.Id}", "index.json");

    public void Persist()
    {
        if (!PersistenceEnabled)
        {
            LastUpdated = DateTime.UtcNow;
            return;
        }

        var indexSerialized = JsonSerializer.Serialize(Index, GetJsonSerializerOptions());
        var knowledgePath = GetKnowledgePath();
        
        var directory = Path.GetDirectoryName(knowledgePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(knowledgePath, indexSerialized);
        LastUpdated = DateTime.UtcNow;
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        if (!PersistenceEnabled)
        {
            LastUpdated = DateTime.UtcNow;
            return;
        }

        var indexSerialized = JsonSerializer.Serialize(Index, GetJsonSerializerOptions());
        var knowledgePath = GetKnowledgePath();
        
        var directory = Path.GetDirectoryName(knowledgePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(knowledgePath, indexSerialized, cancellationToken);
        LastUpdated = DateTime.UtcNow;
    }

    public void Load()
    {
        if (!PersistenceEnabled)
        {
            LastUpdated = DateTime.UtcNow;
            return;
        }

        var knowledgePath = GetKnowledgePath();
        
        if (!File.Exists(knowledgePath))
        {
            throw new FileNotFoundException($"Knowledge index file not found at: {knowledgePath}");
        }
        
        var indexSerialized = File.ReadAllText(knowledgePath);
        Index = JsonSerializer.Deserialize<KnowledgeIndex>(indexSerialized, GetJsonSerializerOptions()) 
                ?? throw new InvalidOperationException("Failed to deserialize knowledge index");
        LastUpdated = DateTime.UtcNow;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!PersistenceEnabled)
        {
            LastUpdated = DateTime.UtcNow;
            return;
        }

        var knowledgePath = GetKnowledgePath();
        
        if (!File.Exists(knowledgePath))
        {
            throw new FileNotFoundException($"Knowledge index file not found at: {knowledgePath}");
        }
        
        var indexSerialized = await File.ReadAllTextAsync(knowledgePath, cancellationToken);
        Index = JsonSerializer.Deserialize<KnowledgeIndex>(indexSerialized, GetJsonSerializerOptions()) 
                ?? throw new InvalidOperationException("Failed to deserialize knowledge index");
        LastUpdated = DateTime.UtcNow;
    }

    public void AddItem(KnowledgeIndexItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Index.Items.Add(item);
    }

    public bool RemoveItem(KnowledgeIndexItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Index.Items.Remove(item);
    }

    public bool RemoveItemByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var item = Index.Items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return item != null && Index.Items.Remove(item);
    }

    public void Clear()
    {
        Index.Items.Clear();
    }

    public void SetIndex(KnowledgeIndex index)
    {
        Index = index ?? throw new ArgumentNullException(nameof(index));
    }

    public void EnablePersistence()
    {
        PersistenceEnabled = true;
    }

    public void DisablePersistence()
    {
        PersistenceEnabled = false;
    }

    public void SetPersistence(bool enabled)
    {
        PersistenceEnabled = enabled;
    }

    public IReadOnlyList<KnowledgeIndexItem> GetItemsByType(KnowledgeItemType type)
    {
        return Index.Items.Where(item => item.Type == type).ToList().AsReadOnly();
    }

    public IReadOnlyList<KnowledgeIndexItem> GetItemsByTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return Index.Items.Where(item => item.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}