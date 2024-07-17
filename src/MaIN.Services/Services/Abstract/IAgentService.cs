using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Models;
using MaIN.Models.Rag;

namespace MaIN.Services.Services.Abstract;

public interface IAgentService
{
    Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false);
    Task<Agent> CreateAgent(Agent agent);
    Task<List<Agent>> GetAgents();
    Task<Agent> GetAgentById(string id);
}