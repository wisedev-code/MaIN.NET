using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Services.Services.Models.Utils;

public class KnowledgeIndexCheckResult
{
    public List<KnowledgeIndexItem> FetchedItems { get; set; } = [];
}