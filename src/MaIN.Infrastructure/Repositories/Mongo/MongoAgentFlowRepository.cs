using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories.Mongo;

public class MongoAgentFlowRepository(IMongoDatabase database, string collectionName) : IAgentFlowRepository
{
    private readonly IMongoCollection<AgentFlowDocument> _flows = database.GetCollection<AgentFlowDocument>(collectionName);

    public async Task<IEnumerable<AgentFlow>> GetAllFlows() =>
        (await _flows.Find(flow => true).ToListAsync()).Select(d => d.ToDomain());

    public async Task<AgentFlow?> GetFlowById(string id) =>
        (await _flows.Find(flow => flow.Id == id).FirstOrDefaultAsync())?.ToDomain();

    public async Task AddFlow(AgentFlow flow) =>
        await _flows.InsertOneAsync(flow.ToDocument());

    public async Task UpdateFlow(string id, AgentFlow flow) =>
        await _flows.ReplaceOneAsync(x => x.Id == id, flow.ToDocument());

    public async Task DeleteFlow(string id) =>
        await _flows.DeleteOneAsync(x => x.Id == id);
}
