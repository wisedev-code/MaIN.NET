using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.ImageGenServices;

namespace MaIN.Services.Utils;

public static class AgentStateManager
{
    public static void ClearState(Agent agent, Chat chat)
    {
        agent.CurrentBehaviour = "Default";
        chat.Properties.Clear();

        if (chat.ModelId == ImageGenService.LocalImageModels.FLUX)
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
