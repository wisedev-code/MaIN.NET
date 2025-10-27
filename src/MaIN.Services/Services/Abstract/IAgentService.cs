using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Models;
using MaIN.Services.Services.LLMService;

namespace MaIN.Services.Services.Abstract;

public interface IAgentService
{
    Task<Chat> Process(Chat chat, string agentId, Knowledge? knowledge, bool translatePrompt = false,
        Func<LLMTokenValue, Task>? callback = null);
    Task<Agent> CreateAgent(Agent agent, bool flow = false, bool interactiveResponse = false,
        InferenceParams? inferenceParams = null, MemoryParams? memoryParams = null, bool disableCache = false);
    Task<Chat> GetChatByAgent(string agentId);
    Task<Chat> Restart(string agentId);
    Task<List<Agent>> GetAgents();
    Task<Agent?> GetAgentById(string id);
    Task DeleteAgent(string id);
    Task<bool> AgentExists(string id);
}