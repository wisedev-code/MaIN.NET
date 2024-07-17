using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Models;
using MaIN.Models.Rag;
using MaIN.Services.Mappers;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;

namespace MaIN.Services.Services;

public class AgentService(
  IOllamaService ollamaService,
  IAgentRepository agentRepository,
  IChatRepository chatRepository) : IAgentService
{
   public async Task<Chat?> Process(Chat? chat, string agentId, bool translatePrompt = false)
{
    // Fetch the agent details from the repository
    var agent = await agentRepository.GetAgentById(agentId);
    
    // Ensure the agent and its context are valid
    if (agent == null)
    {
        throw new ArgumentException("Agent not found.");
    }
    
    var context = agent.Context;
    if (context == null)
    {
        throw new ArgumentException("Agent context not found.");
    }
    
    // Process each step in the defined order
    foreach (var step in context.Steps)
    {
        // Parse the step name and potential id for redirection steps
        var stepParts = step.Split('_');
        var stepName = stepParts[0];
        var relatedAgentId = stepParts.Length > 1 ? stepParts[1] : null;
        
        // Create the appropriate command based on the step name
        switch (stepName)
        {
            case "REDIRECT":
                var redirectCommand = new RedirectCommand
                {
                    Chat = chat!,
                    RelatedAgentId = relatedAgentId
                };
                chat = await Actions.CallAsync("REDIRECT", redirectCommand) as Chat;
                break;
            
            case "FETCH_DATA_WITH_FILTER":
                var fetchCommand = new FetchCommand
                {
                    Chat = chat,
                    //Filter = context.Source.Details.Query // Assuming the query is used as a filter
                };
                chat = await Actions.CallAsync("FETCH_DATA_WITH_FILTER", fetchCommand) as Chat;
                break;
            
            case "ANSWER":
                var answerCommand = new AnswerCommand
                {
                    Chat = chat
                };
                chat = await Actions.CallAsync("ANSWER", answerCommand) as Chat;
                break;
            
            default:
                throw new InvalidOperationException($"Unknown step: {stepName}");
        }
    }
    
    // Return the processed chat
    return chat;
}

    public async Task<Agent> CreateAgent(Agent agent)
    {
        var chat = new Chat()
        {
            Id = Guid.NewGuid().ToString(),
            Model = agent.Model,
            Name = agent.Name,
            Stream = false,
            Messages = new List<Message>(),
            Type = ChatType.Rag,
        };
        
        var startCommand = new StartCommand()
        {
            Chat = chat,
            InitialPrompt = agent.Context.Instruction,
        };
        
        var result = await Actions.CallAsync("START", startCommand) as Chat;
        agent.Started = true;
        var agentDocument = agent.ToDocument();
        agentDocument.ChatId = result?.Id;
        await chatRepository.AddChat(result!.ToDocument());
        await agentRepository.AddAgent(agentDocument);
        return agent;
    }

    public async Task<List<Agent>> GetAgents()
    {
      var result = await agentRepository.GetAllAgents();
      return result
        .Select(x => x.ToDomain())
        .ToList();
    }

    public async Task<Agent> GetAgentById(string id)
    {
      var result = await agentRepository.GetAgentById(id);
      return result.ToDomain();
    }
}