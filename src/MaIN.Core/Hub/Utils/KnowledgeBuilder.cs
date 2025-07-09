using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Core.Hub.Utils;

public class KnowledgeBuilder
{
    private Agent? _agent;
    private KnowledgeIndex? _index;
    private bool _persistenceEnabled = true;
    private readonly List<KnowledgeIndexItem> _items = new();

    public static KnowledgeBuilder Instance => new();

    public KnowledgeBuilder ForAgent(Agent agent)
    {
        _agent = agent;
        return this;
    }

    public KnowledgeBuilder WithIndex(KnowledgeIndex index)
    {
        _index = index;
        return this;
    }

    public KnowledgeBuilder EnablePersistence()
    {
        _persistenceEnabled = true;
        return this;
    }

    public KnowledgeBuilder DisablePersistence()
    {
        _persistenceEnabled = false;
        return this;
    }

    public KnowledgeBuilder WithPersistence(bool enabled)
    {
        _persistenceEnabled = enabled;
        return this;
    }

    public KnowledgeBuilder AddFile(string name, string path, params string[] tags)
    {
        _items.Add(new KnowledgeIndexItem
        {
            Name = name,
            Path = path,
            Type = KnowledgeItemType.File,
            Tags = tags
        });
        return this;
    }

    public KnowledgeBuilder AddUrl(string name, string url, params string[] tags)
    {
        _items.Add(new KnowledgeIndexItem
        {
            Name = name,
            Path = url,
            Type = KnowledgeItemType.Url,
            Tags = tags
        });
        return this;
    }

    public KnowledgeBuilder AddText(string name, string content, params string[] tags)
    {
        _items.Add(new KnowledgeIndexItem
        {
            Name = name,
            Path = content,
            Type = KnowledgeItemType.Text,
            Tags = tags
        });
        return this;
    }

    public KnowledgeBuilder AddItem(string name, string path, KnowledgeItemType type, params string[] tags)
    {
        _items.Add(new KnowledgeIndexItem
        {
            Name = name,
            Path = path,
            Type = type,
            Tags = tags
        });
        return this;
    }

    public KnowledgeBuilder AddItem(KnowledgeIndexItem item)
    {
        _items.Add(item);
        return this;
    }

    public KnowledgeBuilder AddItems(IEnumerable<KnowledgeIndexItem> items)
    {
        _items.AddRange(items);
        return this;
    }

    public Knowledge Build()
    {
        if (_agent == null)
        {
            throw new InvalidOperationException("Agent is required. Use ForAgent() to set the agent.");
        }

        var index = _index ?? new KnowledgeIndex();
        
        if (_items.Any())
        {
            index.Items.AddRange(_items);
        }

        var knowledge = new Knowledge(_agent, index, _persistenceEnabled);
        
        if (_persistenceEnabled && (_items.Any() || _index != null))
        {
            knowledge.Persist();
        }

        return knowledge;
    }
}