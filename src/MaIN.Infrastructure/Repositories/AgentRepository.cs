using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MongoDB.Driver;

namespace MaIN.Infrastructure.Repositories;

public class AgentRepository(IMongoDatabase database, string collectionName) : IAgentRepository
{
    private readonly IMongoCollection<AgentDocument> _agents = database.GetCollection<AgentDocument>(collectionName);

    public async Task<IEnumerable<AgentDocument>> GetAllAgents() =>
        await _agents.Find(chat => true).ToListAsync();

    public async Task<AgentDocument> GetAgentById(string id) => 
        await _agents.Find<AgentDocument>(agent => agent.Id == id).FirstOrDefaultAsync();

    public async Task AddAgent(AgentDocument agent) =>
        await _agents.InsertOneAsync(agent);    
    
    public async Task UpdateAgent(string id, AgentDocument agent) =>
        await _agents.ReplaceOneAsync(x => x.Id == id, agent);
    
    public async Task DeleteAgent(string id) =>
        await _agents.DeleteOneAsync(x => x.Id == id);
    
}