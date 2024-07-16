using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Models;
using MaIN.Models.Rag;

namespace MaIN.Services.Services.Abstract;

public interface IAgentService
{
    Task<Chat> Completions(Chat chat, bool translatePrompt = false);
    Task<Agent> CreateAgent(Agent agent);
    Task<List<Agent>> GetAgents();
}