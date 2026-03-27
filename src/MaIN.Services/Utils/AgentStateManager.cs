using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Services.Utils;

public static class AgentStateManager
{
    public static void ClearState(Agent agent, Chat chat)
    {
        agent.CurrentBehaviour = "Default";
        chat.Properties.Clear();

        if (ModelRegistry.TryGetById(chat.ModelId, out var model) && model!.HasImageGeneration)
        {
            chat.Messages = [];
        }
        else
        {
            chat.Messages[0].Content = agent.Config!.Instruction!;
            chat.Messages = [.. chat.Messages.Take(1)];
        }
    }
}
