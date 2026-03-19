using MaIN.Domain.Entities.Agents;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Domain.Repositories;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories.Mongo;

public class MongoAgentRepository(IMongoDatabase database, string collectionName) : IAgentRepository
{
    private readonly IMongoCollection<AgentDocument> _agents = database.GetCollection<AgentDocument>(collectionName)!;

    public async Task<IEnumerable<Agent>> GetAllAgents() =>
        (await _agents.Find(agent => true).ToListAsync()).Select(d => d.ToDomain());

    public async Task<Agent?> GetAgentById(string id) =>
        (await _agents.Find<AgentDocument>(agent => agent.Id == id).FirstOrDefaultAsync())?.ToDomain();

    public async Task AddAgent(Agent agent) =>
        await _agents.InsertOneAsync(agent.ToDocument());

    public async Task UpdateAgent(string id, Agent agent) =>
        await _agents.ReplaceOneAsync(x => x.Id == id, agent.ToDocument());

    public async Task DeleteAgent(string id) =>
        await _agents.DeleteOneAsync(x => x.Id == id);

    public async Task<bool> Exists(string id) =>
        (await _agents.CountDocumentsAsync(x => x.Id == id)) > 0;
}
