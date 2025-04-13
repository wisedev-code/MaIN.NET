using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Services.Services.ImageGenServices;

namespace MaIN.Services.Utils;

public static class AgentStateManager
{
    public static void ClearState(AgentDocument agent, Chat chat)
    {
        agent.CurrentBehaviour = "Default";
        chat.Properties.Clear();
        
        if (chat.Model == ImageGenService.LocalImageModels.FLUX)
        {
            chat.Messages = [];
        }
        else
        {
            chat.Messages[0].Content = agent.Context!.Instruction!;
            chat.Messages = chat.Messages.Take(1).ToList();
        }
    }
}