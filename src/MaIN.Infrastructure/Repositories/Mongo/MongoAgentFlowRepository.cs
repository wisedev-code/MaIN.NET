using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories;

public class MongoAgentFlowRepository(IMongoDatabase database, string collectionName) : IAgentFlowRepository
{
    private readonly IMongoCollection<AgentFlowDocument> _flows = database.GetCollection<AgentFlowDocument>(collectionName);

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows() =>
        await _flows.Find(chat => true).ToListAsync();

    public async Task<AgentFlowDocument> GetFlowById(string id) =>
        await _flows.Find(flow => flow.Id == id).FirstOrDefaultAsync();

    public async Task AddFlow(AgentFlowDocument flow) =>
        await _flows.InsertOneAsync(flow);

    public async Task UpdateFlow(string id, AgentFlowDocument flow) =>
        await _flows.ReplaceOneAsync(x => x.Id == id, flow);
    
    public async Task DeleteFlow(string id) =>
        await _flows.DeleteOneAsync(x => x.Id == id);
    
}