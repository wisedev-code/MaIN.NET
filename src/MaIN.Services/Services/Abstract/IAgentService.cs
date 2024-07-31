using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;

namespace MaIN.Services.Services.Abstract;

public interface IAgentService
{
    Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false);
    Task<Agent> CreateAgent(Agent agent);
    Task<Chat> GetChatByAgent(string agentId);
    Task<List<Agent>> GetAgents();
    Task<Agent?> GetAgentById(string id);
}