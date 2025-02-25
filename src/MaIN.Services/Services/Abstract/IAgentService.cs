using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace MaIN.Services.Services.Abstract;

public interface IAgentService
{
    Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false);
    Task<Agent> CreateAgent(Agent agent, bool flow = false, bool interactiveResponse = false);
    Task<Chat?> GetChatByAgent(string agentId);
    Task<Chat?> Restart(string agentId);
    Task<List<Agent>> GetAgents();
    Task<Agent?> GetAgentById(string id);
    Task DeleteAgent(string id);
    Task<bool> AgentExists(string id);
}